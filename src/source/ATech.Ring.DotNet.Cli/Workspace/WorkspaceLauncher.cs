using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions;
using ATech.Ring.DotNet.Cli.Dtos;
using ATech.Ring.DotNet.Cli.Logging;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Workspace
{
    public class WorkspaceLauncher : IWorkspaceLauncher, IDisposable
    {
        private readonly IConfigurator _configurator;
        private readonly ILogger<WorkspaceLauncher> _logger;
        private readonly Func<IRunnableConfig, IRunnable> _createRunnable;
        private readonly IWorkspaceInitHook _initHook;
        private readonly ISender<IRingEvent> _sender;
        private readonly ConcurrentDictionary<string, RunnableContainer> _runnables = new ConcurrentDictionary<string, RunnableContainer>();
        private Task _startTask;
        private Task _stopTask;
        private Task _initHookTask;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private WorkspaceInfo CurrentInfo { get; set; }
        private WorkspaceState CurrentStatus { get; set; }
        private int _initCounter;
        private readonly Random _rnd = new Random();

        public event EventHandler OnInitiated;
        public string WorkspacePath => _configurator.Current.Path;

        public WorkspaceLauncher(IConfigurator configurator,
                                 ILogger<WorkspaceLauncher> logger,
                                 Func<IRunnableConfig, IRunnable> createRunnable,
                                 IWorkspaceInitHook initHook,
                                 ISender<IRingEvent> sender)
        {
            _configurator = configurator;
            _logger = logger;
            _createRunnable = createRunnable;
            _initHook = initHook;
            _sender = sender;
            OnInitiated += WorkspaceLauncher_OnInitiated;
        }

        private void WorkspaceLauncher_OnInitiated(object sender, EventArgs e)
        {
            _initHookTask = _initHook.RunAsync(_cts.Token);
        }

        public async Task LoadAsync(ConfiguratorPaths paths, CancellationToken token)
        {
            _configurator.OnConfigurationChanged += async (_, args) => { await ApplyConfigChanges(args.Configuration, _cts.Token); };

            var configDirectory = new FileInfo(paths.WorkspacePath).DirectoryName;
            Directory.SetCurrentDirectory(configDirectory);

            await _configurator.LoadAsync(paths, _cts.Token);
            using (_logger.WithHostScope(Phase.INIT))
            {
                _logger.LogInformation(PhaseStatus.OK);
            }
        }

        public async Task StartAsync(CancellationToken token)
        {
            _cts = new CancellationTokenSource();
            _startTask = await Task.Factory.StartNew(async () => await ApplyConfigChanges(_configurator.Current, _cts.Token), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task UnloadAsync(CancellationToken token)
        {
            await _configurator.UnloadAsync(_cts.Token);
        }

        public async Task StopAsync(CancellationToken token)
        {
            _cts.Cancel();
            await _startTask;
            if (_initHookTask != null) await _initHookTask;
            _initCounter = 0;
            _stopTask = Task.WhenAll(_runnables.Keys.Select(RemoveAsync));
        }

        public async Task<ExcludeResult> ExcludeAsync(string id, CancellationToken token)
        {
            return await RemoveAsync(id) ? ExcludeResult.Ok : ExcludeResult.UnknownRunnable;
        }

        public async Task<IncludeResult> IncludeAsync(string id, CancellationToken token)
        {
            if (!_configurator.TryGet(id, out var cfg)) return IncludeResult.UnknownRunnable;
            await AddAsync(id, cfg, _cts.Token);
            return IncludeResult.Ok;
        }

        public void PublishStatus(ServerState serverState) => PublishStatusCore(serverState, force: true);

        private async Task ApplyConfigChanges(IDictionary<string, IRunnableConfig> configs, CancellationToken token)
        {
            try
            {
                var deletions = _runnables.Keys.Except(configs.Keys).ToArray();
                var deletionsTask = deletions.Select(RemoveAsync);
                var additions = configs.Keys.Except(_runnables.Keys).ToArray();
                var additionsTask = additions.Select(key => AddAsync(key, configs[key], token));

                await Task.WhenAll(additionsTask.Concat(deletionsTask));
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Workspace cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
            }
        }

        private TimeSpan CaclulateDelay()
        {
            const int spreadFactorMillis = 1_500;
            lock (_rnd)
            {
                return TimeSpan.FromMilliseconds(_rnd.Next(0, _configurator.Current.Count * spreadFactorMillis));
            }
        }

        private WorkspaceInfo Create(WorkspaceState state, ServerState serverState)
        {
            return new WorkspaceInfo(WorkspacePath,
                _configurator.Current.Select(entry =>
                {
                    var (id, cfg) = entry;
                    var isRunning = _runnables.TryGetValue(id, out var container);

                    var runnableState = isRunning ? container.Runnable switch
                    {
                        { State: State.Zero } => RunnableState.ZERO,
                        { State: State.Idle } => RunnableState.INITIATED,
                        { State: State.ProbingHealth } => RunnableState.HEALTH_CHECK,
                        { State: State.Healthy } => RunnableState.HEALTHY,
                        { State: State.Dead } => RunnableState.DEAD,
                        { State: State.Recovering } => RunnableState.RECOVERING,
                        { State: State.Pending } => RunnableState.STARTED,
                        _ => RunnableState.ZERO
                    } : RunnableState.ZERO;

                    var details = isRunning ? container.Runnable.Details : DetailsExtractors.Extract(cfg);

                    var runnableInfo = new RunnableInfo(id, cfg.DeclaredPaths.ToArray(), cfg.GetType().Name, runnableState, details);

                    return runnableInfo;

                }).OrderBy(x => x.Id).ToArray(), serverState, state);
        }

        private async Task AddAsync(string id, IRunnableConfig cfg, CancellationToken token)
        {
            if (_runnables.ContainsKey(id)) return;
            var container = await RunnableContainer.CreateAsync(cfg, _createRunnable, CaclulateDelay(), token);
            _runnables.TryAdd(id, container);
            container.Runnable.OnHealthCheckCompleted += OnPublishStatus;
            container.Runnable.OnInitExecuted += OnRunnableInitExecuted;
        }

        private async Task<bool> RemoveAsync(string key)
        {
            if (!_runnables.TryRemove(key, out var r)) return false;

            Interlocked.Decrement(ref _initCounter);
            r.Runnable.OnHealthCheckCompleted -= OnPublishStatus;
            r.Runnable.OnInitExecuted -= OnRunnableInitExecuted;
            await r.CancelAsync();
            r.Dispose();
            return true;
        }

        private void OnRunnableInitExecuted(object sender, EventArgs e)
        {
            if (_runnables.Count != Interlocked.Increment(ref _initCounter)) return;
            using var _ = _logger.WithHostScope(Phase.INIT);
            OnInitiated?.Invoke(this, EventArgs.Empty);
        }
        private void OnPublishStatus(object sender, EventArgs args) => PublishStatusCore(ServerState.RUNNING, force: false);

        private void PublishStatusCore(ServerState serverState, bool force)
        {
            var state = serverState == ServerState.IDLE ? WorkspaceState.NONE :
                        !_runnables.Any() ? WorkspaceState.IDLE :
                        _runnables.Values.Select(x => x.Runnable).All(r => r.State == State.ProbingHealth || r.State == State.Healthy) ? WorkspaceState.HEALTHY :
                        WorkspaceState.DEGRADED;

            if (!force && state == CurrentStatus) return;
            CurrentStatus = state;
            var info = Create(state, serverState);
            if (!force && info.Equals(CurrentInfo)) return;
            CurrentInfo = info;
            _sender.Enqueue(new WorkspaceInfoPub { WorkspaceInfoJson = CurrentInfo });
        }

        public void Dispose() => _cts?.Dispose();

        public async ValueTask DisposeAsync() => await _stopTask;
    }
}