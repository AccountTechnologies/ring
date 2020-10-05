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
        private readonly ConcurrentDictionary<string, IRunnable> _runnables = new ConcurrentDictionary<string, IRunnable>();
        private readonly ConcurrentDictionary<string, Task> _tasks = new ConcurrentDictionary<string, Task>();
        private Task _workspaceTask;
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
            _workspaceTask = await Task.Factory.StartNew(async () => await ApplyConfigChanges(_configurator.Current, _cts.Token), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task UnloadAsync(CancellationToken token)
        {
            await _configurator.UnloadAsync(_cts.Token);
        }

        public async Task StopAsync(CancellationToken token)
        {
            _cts.Cancel();
            await ForEachParallel(_runnables.Keys, _cts.Token, RemoveAsync);
            await _workspaceTask;
            if (_initHookTask != null) await _initHookTask;
            _initCounter = 0;
        }

        public async Task<ExcludeResult> ExcludeAsync(string id, CancellationToken token)
        {
            return await RemoveAsync(id, _cts.Token) ? ExcludeResult.Ok : ExcludeResult.UnknownRunnable;
        }

        public Task<IncludeResult> IncludeAsync(string id, CancellationToken token)
        {
            if (!_configurator.TryGet(id, out var cfg)) return Task.FromResult(IncludeResult.UnknownRunnable);
            _tasks.TryAdd(id, AddAsync(id, cfg, _cts.Token));
            return Task.FromResult(IncludeResult.Ok);
        }

        public void PublishStatus(ServerState serverState) => PublishStatusCore(serverState, force: true);

        private async Task ApplyConfigChanges(IDictionary<string, IRunnableConfig> configs, CancellationToken token)
        {
            try
            {
                var deletions = _runnables.Keys.Except(configs.Keys).ToArray();
                var deletionsTask = ForEachParallel(deletions, token, RemoveAsync);
                var additions = configs.Keys.Except(_runnables.Keys).ToArray();
                var additionsTask = ForEachParallel(additions, token, async (key, t) =>
                {
                    var cfg = configs[key];
                    await AddRandomDelay(t);
                    await AddAsync(key, cfg, t);
                });

                await Task.WhenAll(deletionsTask, additionsTask);
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

        private async Task AddRandomDelay(CancellationToken t)
        {
            int delayMs;
            const int spreadFactorMillis = 1_500;
            lock (_rnd)
            {
                delayMs = _rnd.Next(0, _configurator.Current.Count * spreadFactorMillis);
            }
            if (delayMs == 0) return;
            await Task.Delay(TimeSpan.FromMilliseconds(delayMs), t);
        }

        private WorkspaceInfo Create(WorkspaceState state, ServerState serverState)
        {
            return new WorkspaceInfo(WorkspacePath,
                _configurator.Current.Select(entry =>
                {
                    var (id, cfg) = entry;
                    var isRunning = _runnables.TryGetValue(id, out var runnable);

                    var runnableState = runnable switch
                    {
                        { State: State.Zero } => RunnableState.ZERO,
                        { State: State.Idle } => RunnableState.INITIATED,
                        { State: State.ProbingHealth } => RunnableState.HEALTH_CHECK,
                        { State: State.Healthy } => RunnableState.HEALTHY,
                        { State: State.Dead } => RunnableState.DEAD,
                        { State: State.Recovering } => RunnableState.RECOVERING,
                        { State: State.Pending } => RunnableState.STARTED,
                        _ => RunnableState.ZERO
                    };

                    var details = isRunning ? runnable.Details : DetailsExtractors.Extract(cfg);

                    var runnableInfo = new RunnableInfo(id, cfg.DeclaredPaths.ToArray(), cfg.GetType().Name, runnableState, details);

                    return runnableInfo;

                }).OrderBy(x => x.Id).ToArray(), serverState, state);
        }

        private async Task AddAsync(string id, IRunnableConfig cfg, CancellationToken token)
        {
            if (_runnables.ContainsKey(id)) return;

            var runnable = _createRunnable(cfg);
            runnable.OnHealthCheckCompleted += OnPublishStatus;
            runnable.OnInitExecuted += OnRunnableInitExecuted;
            _runnables.TryAdd(id, runnable);

            await await Task.Factory.StartNew(() => runnable.RunAsync(cfg, _cts.Token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private async Task<bool> RemoveAsync(string key, CancellationToken token)
        {
            if (!_runnables.TryRemove(key, out var r)) return false;

            Interlocked.Decrement(ref _initCounter);
            r.OnHealthCheckCompleted -= OnPublishStatus;
            r.OnInitExecuted -= OnRunnableInitExecuted;

            _tasks.TryRemove(key, out var task);

            await r.TerminateAsync(token);
            if (task != null) await task;

            return true;
        }

        private async Task ForEachParallel(IEnumerable<string> keys, CancellationToken token, Func<string, CancellationToken, Task> func)
        {
            if (!keys.Any()) return;
            var pairs = keys.AsParallel().Select(k => (key: k, task: func(k, token)));
            foreach (var (key, task) in pairs) _tasks.TryAdd(key, task);
            var completed = await Task.WhenAny(_tasks.Select(x => x.Value).ToArray());
            await completed;
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
                        _runnables.Values.All(r => r.State == State.ProbingHealth || r.State == State.Healthy) ? WorkspaceState.HEALTHY :
                        WorkspaceState.DEGRADED;

            if (!force && state == CurrentStatus) return;
            CurrentStatus = state;
            var info = Create(state, serverState);
            if (!force && info.Equals(CurrentInfo)) return;
            CurrentInfo = info;
            _sender.Enqueue(new WorkspaceInfoPub { WorkspaceInfoJson = CurrentInfo });
        }

        public void Dispose()
        {
            _cts?.Dispose();
        }
    }
}