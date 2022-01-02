using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions.Context;
using ATech.Ring.DotNet.Cli.Dtos;
using ATech.Ring.DotNet.Cli.Logging;
using ATech.Ring.Protocol.v2;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Abstractions
{
    [DebuggerDisplay("{UniqueId}")]
    public abstract class Runnable<TContext, TConfig> : IRunnable
        where TConfig : IRunnableConfig
    {
        private readonly ILogger<Runnable<TContext, TConfig>> _logger;
        private readonly Fsm _fsm = new();
        private TContext? _context;
        protected readonly ISender Sender;
        protected virtual TimeSpan HealthCheckPeriod { get; } = TimeSpan.FromSeconds(5);
        protected virtual int MaxConsecutiveFailuresUntilDead { get; } = 2;
        protected virtual int MaxTotalFailuresUntilDead { get; } = 3;
        public TConfig Config { get; private set; }
        public abstract string UniqueId { get; }
        public State State => _fsm.State;
        public event EventHandler? OnHealthCheckCompleted;
        public event EventHandler? OnInitExecuted;
        public IReadOnlyDictionary<string, object> Details => _details;
        private readonly Dictionary<string, object> _details = new();
        /// <summary>
        /// Details added via this method are pushed to clients where can be used for different purposes
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected void AddDetail(string key, object value) => _details.TryAdd(key, value);

        protected Runnable(TConfig config, ILogger<Runnable<TContext, TConfig>> logger, ISender sender)
        {
            Config = config;
            if (Config.FriendlyName != null) { _details.Add(DetailsKeys.FriendlyName, Config.FriendlyName);}
            _logger = logger;
            Sender = sender;
        }

        protected abstract Task<TContext> InitAsync(CancellationToken token);
        protected abstract Task StartAsync(TContext ctx, CancellationToken token);
        protected abstract Task<HealthStatus> CheckHealthAsync(TContext ctx, CancellationToken token);
        protected abstract Task StopAsync(TContext ctx, CancellationToken token);
        protected abstract Task DestroyAsync(TContext ctx, CancellationToken token);

        protected virtual async Task RecoverAsync(TContext ctx, CancellationToken token)
        {
            await _fsm.FireAsync(Trigger.Stop);
            await _fsm.FireAsync(Trigger.Start);
        }

        private async Task<Fsm> InitFsm(CancellationToken token)
        {
            _fsm.Configure(State.Zero)
                .OnEntryFromAsync(Trigger.Destroy, async ctx => await DestroyCoreAsync(_context, token))
                .Ignore(Trigger.NoOp)
                .Ignore(Trigger.HcOk)
                .Ignore(Trigger.HcUnhealthy)
                .Permit(Trigger.Init, State.Idle);

            _fsm.Configure(State.Idle)
                .OnEntryFromAsync(Trigger.Init, () => InitCoreAsync(token))
                .OnEntryFromAsync(Trigger.Stop, () => StopCoreAsync(_context, token))
                .Permit(Trigger.Start, State.Pending)
                .Permit(Trigger.InitFailure, State.Pending)
                .Permit(Trigger.Destroy, State.Zero)
                .Ignore(Trigger.HcUnhealthy)
                .Ignore(Trigger.NoOp)
                .Ignore(Trigger.HcOk)
                .Ignore(Trigger.Stop);

            _fsm.Configure(State.Pending)
                .OnEntryFromAsync(Trigger.Start,
                    async () =>
                    {
                        await StartCoreAsync(_context, token);
                        await QueueHealthCheckAsync(token);
                    })
                .OnEntryFromAsync(Trigger.InitFailure, () => QueueHealthCheckAsync(token))
                .Permit(Trigger.HealthLoop, State.ProbingHealth)
                .Permit(Trigger.Stop, State.Idle);

            _fsm.Configure(State.ProbingHealth)
                .OnEntryFromAsync(Trigger.HealthLoop,
                    async () =>
                    {
                        Sender.Enqueue(Message.RunnableHealthCheck(UniqueId));
                        var healthResult = await CheckHealthCoreAsync(_context, token);
                        await _fsm.FireAsync(healthResult switch
                        {
                            HealthStatus.Dead => Trigger.HcDead,
                            HealthStatus.Unhealthy => Trigger.HcUnhealthy,
                            HealthStatus.Ok => Trigger.HcOk,
                            HealthStatus.Ignore => Trigger.NoOp,
                            _ => Trigger.Invalid
                        });
                        OnHealthCheckCompleted?.Invoke(this, EventArgs.Empty);
                    })
                .Permit(Trigger.HcOk, State.Healthy)
                .Permit(Trigger.HcDead, State.Dead)
                .Permit(Trigger.HcUnhealthy, State.Recovering)
                .Permit(Trigger.Stop, State.Idle)
                .Ignore(Trigger.NoOp);

            _fsm.Configure(State.Healthy)
                .OnEntryFromAsync(Trigger.HcOk, async () =>
                {
                    Sender.Enqueue(Message.RunnableHealthy(UniqueId));
                    await QueueHealthCheckAsync(token);
                })
                .Permit(Trigger.HealthLoop, State.ProbingHealth)
                .Permit(Trigger.Stop, State.Idle);

            _fsm.Configure(State.Dead)
                .OnEntryFromAsync(Trigger.HcDead, () =>
                {
                    Sender.Enqueue(Message.RunnableDead(UniqueId));
                    return Task.CompletedTask;
                })
                .Permit(Trigger.Stop, State.Idle);

            _fsm.Configure(State.Recovering)
                .OnEntryFromAsync(Trigger.HcUnhealthy,
                    async () =>
                        {
                            Sender.Enqueue(Message.RunnableRecovering(UniqueId));
                            await RecoverCoreAsync(_context, token);
                        })
                .Permit(Trigger.Stop, State.Idle);

            _fsm.OnTransitioned(t => _logger.LogDebug("{Source} -> {Trigger} -> {Destination}", t.Source, t.Trigger, t.Destination));

            await _fsm.ActivateAsync();
            return _fsm;

            async Task QueueHealthCheckAsync(CancellationToken t)
            {
                try
                {
                    if (t.IsCancellationRequested) return;
                    var delay = Task.Delay(HealthCheckPeriod, t);
                    await delay.ConfigureAwait(false);
                    delay.Dispose();
                    if (t.IsCancellationRequested) return;
                    await _fsm.FireAsync(Trigger.HealthLoop);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Health check cancelled");
                }
            }
        }

        public async Task RunAsync(CancellationToken token)
        {
            using var _ = _logger.BeginScope(this.ToScope());
            var fsm = await InitFsm(token);

            await fsm.FireAsync(Trigger.Init);
        }

        public async Task TerminateAsync()
        {
            using var _ = _logger.BeginScope(this.ToScope());
            await _fsm.FireAsync(Trigger.Stop);
            await _fsm.FireAsync(Trigger.Destroy);
        }

        private async Task InitCoreAsync(CancellationToken token)
        {
            try
            {
                using var _ = _logger.BeginScope(Scope.Phase(Phase.INIT));
                _logger.LogDebug(PhaseStatus.PENDING);
                _context = await InitAsync(token);
                _logger.LogContextDebug(_context);
                _logger.LogDebug(PhaseStatus.OK);
                Sender.Enqueue(Message.RunnableInitiated(UniqueId));
                await _fsm.FireAsync(Trigger.Start);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Initialization failed");
                _context = (TContext) FormatterServices.GetUninitializedObject(typeof(TContext));
                await _fsm.FireAsync(Trigger.InitFailure);
            }
            finally
            {
                OnInitExecuted?.Invoke(this, EventArgs.Empty);
            }
        }
        protected async Task StartCoreAsync(TContext ctx, CancellationToken token)
        {
            using var _ = _logger.BeginScope(Scope.Phase(Phase.START));
            _logger.LogDebug(PhaseStatus.PENDING);
            await StartAsync(ctx, token);
            _logger.LogContextDebug(ctx);
            _logger.LogInformation(PhaseStatus.OK);
            Sender.Enqueue(Message.RunnableStarted(UniqueId));
        }
        private async Task<HealthStatus> CheckHealthCoreAsync(TContext ctx, CancellationToken token)
        {
            try
            {
                using var _ = _logger.BeginScope(Scope.Phase(Phase.HEALTH));
                if (token.IsCancellationRequested) return HealthStatus.Ignore;
                _logger.LogDebug(PhaseStatus.PENDING);
                HealthStatus result;
                try
                {
                    result = await CheckHealthAsync(ctx, token);
                    if (token.IsCancellationRequested) return HealthStatus.Ignore;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected healthcheck error");
                    result = HealthStatus.Unhealthy;
                }

                _logger.LogContextDebug(ctx);

                switch (result)
                {
                    case HealthStatus.Unhealthy:
                        _logger.LogError("UNHEALTHY");
                        break;
                    case HealthStatus.Ok:
                        _logger.LogDebug(PhaseStatus.OK);
                        break;
                    case HealthStatus.Ignore:
                        break;
                    case HealthStatus.Dead:
                        _logger.LogError("DEAD");
                        break;
                    default:
                        throw new NotSupportedException($"Status '{result}' is not supported.");
                }

                if (ctx is not ITrackRetries t) return result;

                if (result == HealthStatus.Ok)
                {
                    t.ConsecutiveFailures = 0;
                    return result;
                }

                t.ConsecutiveFailures++;
                t.TotalFailures++;
                token.ThrowIfCancellationRequested();
                return t.ConsecutiveFailures < MaxConsecutiveFailuresUntilDead
                       && t.TotalFailures < MaxTotalFailuresUntilDead
                    ? result
                    : HealthStatus.Dead;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("HealthCheck cancelled");
                return HealthStatus.Ignore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HealthCheck exception");
                throw;
            }
        }

        private async Task RecoverCoreAsync(TContext ctx, CancellationToken token)
        {
            using var _ = _logger.BeginScope(Scope.Phase(Phase.RECOVERY));
            _logger.LogDebug(PhaseStatus.PENDING);
            await RecoverAsync(ctx, token);
            _logger.LogContextDebug(ctx);
        }

        protected async Task StopCoreAsync(TContext ctx, CancellationToken token)
        {
            using var _ = _logger.BeginScope(Scope.Phase(Phase.STOP));
            _logger.LogDebug(PhaseStatus.PENDING);
            await StopAsync(ctx, token);
            _logger.LogContextDebug(ctx);
            _logger.LogDebug(PhaseStatus.OK);
            Sender.Enqueue(Message.RunnableStopped(UniqueId));
        }
        private async Task DestroyCoreAsync(TContext ctx, CancellationToken token)
        {
            using var _ = _logger.BeginScope(Scope.Phase(Phase.DESTROY));
            _logger.LogDebug(PhaseStatus.PENDING);
            await DestroyAsync(ctx, token);
            _logger.LogContextDebug(ctx);
            _logger.LogInformation(PhaseStatus.OK);
            Sender.Enqueue(Message.RunnableDestroyed(UniqueId));
        }

        private class Fsm : Stateless.StateMachine<State, Trigger>
        {
            public Fsm() : base(State.Zero) { }
        }
    }

    public enum State { Zero, Idle, Pending, ProbingHealth, Healthy, Recovering, Dead }
    public enum Trigger { NoOp, Invalid, Init, InitFailure, Start, Stop, Destroy, HealthLoop, HcUnhealthy, HcOk, HcDead }
}
