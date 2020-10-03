using System;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration;
using ATech.Ring.DotNet.Cli.Dtos;
using ATech.Ring.DotNet.Cli.Logging;
using ATech.Ring.DotNet.Cli.Workspace;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stateless;
using Scope = LightInject.Scope;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    using S = Server.State;
    using T = Server.Trigger;

    public class Server : IServer
    {
        private readonly Func<Scope> _getScope;
        private readonly ILogger<Server> _logger;
        private readonly IWorkspaceLauncher _launcher;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ISender<IRingEvent> _sender;
        private readonly ServerFsm _fsm;
        private Scope _scope;
        public Server(Func<Scope> getScope, ILogger<Server> logger, IWorkspaceLauncher launcher, IHostApplicationLifetime appLifetime, ISender<IRingEvent> sender)
        {
            _getScope = getScope;
            _logger = logger;
            _launcher = launcher;
            _appLifetime = appLifetime;
            _sender = sender;
            _fsm = new ServerFsm();
        }

        public Task InitializeAsync(CancellationToken token)
        {
            _fsm.Configure(S.Idle)
                .OnEntryFromAsync(T.Unload, async () =>
                {
                    await _launcher.UnloadAsync(token);
                    RequestWorkspaceInfo();
                })
              .Ignore(T.Unload)
              .Permit(T.Load, S.Loaded);

            _fsm.Configure(S.Loaded)
                .OnEntryFromAsync(T.Load.Of<string>(), async path =>
                {
                    await _launcher.LoadAsync(new ConfiguratorPaths { WorkspacePath = path }, token);
                    RequestWorkspaceInfo();
                })
                .OnEntryFromAsync(T.Stop, async () =>
                {
                    await _launcher.StopAsync(token);
                    RequestWorkspaceInfo();
                })
                .InternalTransition(T.Include, () => { })
                .InternalTransition(T.Exclude, () => { })
                .Permit(T.Unload, S.Idle)
                .Permit(T.Start, S.Running);

            _fsm.Configure(S.Running)
                    .OnEntryFromAsync(T.Start, async () =>
                    {
                        _scope = _getScope();
                        await _launcher.StartAsync(token);
                    })
                    .InternalTransition(T.Include, () => { })
                    .InternalTransition(T.Exclude, () => { })
                    .Permit(T.Stop, S.Loaded);

            _fsm.OnUnhandledTrigger((s, t) => _logger.LogInformation("Trigger: {trigger} is not supported in state: {state}", t, s));
            return Task.CompletedTask;
        }

        public Task<Ack> ConnectAsync(CancellationToken token)
        {
            _sender.Enqueue(_fsm.State switch
            {
                S.Idle => new ServerIdle(),
                S.Loaded => new ServerLoaded { WorkspacePath = _launcher.WorkspacePath },
                S.Running => new ServerRunning { WorkspacePath = _launcher.WorkspacePath },
                _ => default
            });
            return Task.FromResult(Ack.Ok);
        }

        public async Task<Ack> LoadAsync(string path, CancellationToken token)
        {
            await _fsm.FireAsync(T.Load.Of<string>(), path);
            return Ack.Ok;
        }

        public async Task<Ack> UnloadAsync(CancellationToken token)
        {
            if (_fsm.CanFire(T.Stop)) await _fsm.FireAsync(T.Stop);
            if (_fsm.CanFire(T.Unload)) await _fsm.FireAsync(T.Unload);
            return Ack.Ok;
        }

        public async Task<Ack> TerminateAsync(CancellationToken token)
        {
            await UnloadAsync(token);

            _scope?.Dispose();
            using var _ = _logger.WithHostScope(Phase.DESTROY);
            _logger.LogDebug("Server terminating");
            if (!_appLifetime.ApplicationStopping.IsCancellationRequested) _appLifetime.StopApplication();
            return Ack.Ok;
        }

        public async Task<Ack> IncludeAsync(string id, CancellationToken token)
        {
            await _fsm.FireAsync(T.Include);
            return await _launcher.IncludeAsync(id, token) == IncludeResult.UnknownRunnable ? Ack.NotFound : Ack.Ok;
        }

        public Ack RequestWorkspaceInfo()
        {
            _launcher.PublishStatus(_fsm.State switch
            {
                S.Idle => ServerState.IDLE,
                S.Loaded => ServerState.LOADED,
                S.Running => ServerState.RUNNING,
                _ => throw new NotSupportedException($"State {_fsm.State} not supported")
            });

            return Ack.Ok;
        }

        public async Task<Ack> ExcludeAsync(string id, CancellationToken token)
        {
            await _fsm.FireAsync(T.Exclude);
            return await _launcher.ExcludeAsync(id, token) == ExcludeResult.UnknownRunnable ? Ack.NotFound : Ack.Ok;
        }

        public async Task<Ack> StartAsync(CancellationToken token)
        {
            await _fsm.FireAsync(T.Start);
            return Ack.Ok;
        }

        public async Task<Ack> StopAsync(CancellationToken token)
        {
            await _fsm.FireAsync(T.Stop);
            return Ack.Ok;
        }

        internal enum State
        {
            Idle,
            Loaded,
            Running
        }

        internal enum Trigger
        {
            Load,
            Unload,
            Terminate,
            Include,
            Exclude,
            Start,
            Stop
        }

        internal class ServerFsm : StateMachine<S, T>
        {
            public ServerFsm() : base(S.Idle)
            {
            }
        }
    }

    internal static class StateMachineExtensions
    {
        internal static Server.ServerFsm.TriggerWithParameters<T> Of<T>(this Server.Trigger t) => new StateMachine<S, Server.Trigger>.TriggerWithParameters<T>(t);
    }
}