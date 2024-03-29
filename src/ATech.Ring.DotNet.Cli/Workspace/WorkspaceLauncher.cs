﻿using System;
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
using ATech.Ring.DotNet.Cli.Infrastructure;
using ATech.Ring.DotNet.Cli.Logging;
using ATech.Ring.Protocol.v2;
using ATech.Ring.Protocol.v2.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ATech.Ring.DotNet.Cli.Workspace;

public sealed class WorkspaceLauncher : IWorkspaceLauncher, IDisposable
{
    private readonly IConfigurator _configurator;
    private readonly ILogger<WorkspaceLauncher> _logger;
    private readonly Func<IRunnableConfig, IRunnable> _createRunnable;
    private readonly IWorkspaceInitHook _initHook;
    private readonly ISender _sender;
    private readonly int _spreadFactor;
    private readonly ConcurrentDictionary<string, RunnableContainer> _runnables = new();
    private Task? _startTask;
    private Task? _stopTask;
    private Task? _initHookTask;
    private CancellationTokenSource _cts = new();
    private WorkspaceInfo CurrentInfo { get; set; } = WorkspaceInfo.Empty;
    private WorkspaceState CurrentStatus { get; set; }
    private string CurrentFlavour { get; set; } = ConfigSet.AllFlavours;
    private int _initCounter;
    private readonly Random _rnd = new();

    public event EventHandler OnInitiated;
    public async Task<ApplyFlavourResult> ApplyFlavourAsync(string flavour, CancellationToken token)
    {
        if (CurrentFlavour == flavour) return ApplyFlavourResult.Ok;
        if (!CurrentInfo.Flavours.Contains(flavour)) return ApplyFlavourResult.UnknownFlavour;
        var candidates = _configurator.Current.Select(x => (x.Key, x.Value.Tags.Contains(flavour)));
        foreach (var (key, inFlavour) in candidates)
        {
            if (inFlavour)
            {
                await IncludeAsync(key, token);
            }
            else
            {
                await ExcludeAsync(key, token);
            }
        }

        CurrentFlavour = flavour;
        PublishStatusCore(ServerState.RUNNING, force: true);
        return ApplyFlavourResult.Ok;
    }

    public string WorkspacePath => _configurator.Current.Path;

    public WorkspaceLauncher(IConfigurator configurator,
        ILogger<WorkspaceLauncher> logger,
        Func<IRunnableConfig, IRunnable> createRunnable,
        IWorkspaceInitHook initHook,
        ISender sender,
        IOptions<RingConfiguration> options)
    {
        _configurator = configurator;
        _logger = logger;
        _createRunnable = createRunnable;
        _initHook = initHook;
        _sender = sender;
        _spreadFactor = options.Value.Workspace.StartupSpreadFactor;
        OnInitiated += WorkspaceLauncher_OnInitiated;
    }

    private void WorkspaceLauncher_OnInitiated(object? sender, EventArgs e)
    {
        _initHookTask = _initHook.RunAsync(_cts.Token);
    }

    public async Task LoadAsync(ConfiguratorPaths paths, CancellationToken token)
    {
        _configurator.OnConfigurationChanged += async (_, args) => { await ApplyConfigChanges(args.Configuration, _cts.Token); };

        var configDirectory = new FileInfo(paths.WorkspacePath).DirectoryName;
        if (configDirectory == null) 
        {
            throw new InvalidOperationException($"Path '{configDirectory}' does not have directory name");
        }
        Directory.SetCurrentDirectory(configDirectory);

        await _configurator.LoadAsync(paths, _cts.Token);
        using (_logger.WithHostScope(Phase.INIT))
        {
            _logger.LogInformation(PhaseStatus.OK);
        }
    }

    public Task StartAsync(CancellationToken token)
    {
        _cts = new CancellationTokenSource();
        _startTask = Task.Factory.StartNew(async () => await ApplyConfigChanges(_configurator.Current, _cts.Token), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        return Task.CompletedTask;
    }

    public async Task UnloadAsync(CancellationToken token)
    {
        await _configurator.UnloadAsync(_cts.Token);
    }

    public async Task StopAsync(CancellationToken token)
    {
        _cts.Cancel();
        if (_initHookTask != null) await _initHookTask;
        _initCounter = 0;
        _stopTask = Task.WhenAll(_runnables.Keys.Select(RemoveAsync));
        await (_startTask ?? throw new InvalidOperationException($"{nameof(_startTask)} must not be null"));
    }

    public async Task<ExcludeResult> ExcludeAsync(string id, CancellationToken token)
    {
        return await RemoveAsync(id) ? ExcludeResult.Ok : ExcludeResult.UnknownRunnable;
    }

    public async Task<IncludeResult> IncludeAsync(string id, CancellationToken token)
    {
        if (!_configurator.TryGet(id, out var cfg)) return IncludeResult.UnknownRunnable;
        await AddAsync(id, cfg, TimeSpan.Zero, _cts.Token);
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
            var additionsTask = additions.Select(key => {
                var delay = CalculateDelay(additions.Length);
                _logger.LogDebug("Starting in {delay}", delay);
                return AddAsync(key, configs[key], delay, token);
            });

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

    private TimeSpan CalculateDelay(int runnablesCount)
    {
        lock (_rnd)
        {
            return TimeSpan.FromMilliseconds(runnablesCount <= 7 ? 1000 : _rnd.Next(0, Math.Max(runnablesCount - 1, 0) * _spreadFactor));
        }
    }

    private WorkspaceInfo Create(WorkspaceState state, ServerState serverState)
    {
        var runnables = _configurator.Current.Select(entry =>
        {
            var (id, cfg) = entry;
            var isRunning = _runnables.TryGetValue(id, out var container);

            var runnableState = isRunning
                ? container!.Runnable switch
                {
                    { State: State.Zero } => RunnableState.ZERO,
                    { State: State.Idle } => RunnableState.INITIATED,
                    { State: State.ProbingHealth } => RunnableState.HEALTH_CHECK,
                    { State: State.Healthy } => RunnableState.HEALTHY,
                    { State: State.Dead } => RunnableState.DEAD,
                    { State: State.Recovering } => RunnableState.RECOVERING,
                    { State: State.Pending } => RunnableState.STARTED,
                    _ => RunnableState.ZERO
                }
                : RunnableState.ZERO;

            var details = isRunning ? container!.Runnable.Details : DetailsExtractors.Extract(cfg);

            var runnableInfo = new RunnableInfo(id, 
                cfg.DeclaredPaths.ToArray(), 
                cfg.GetType().Name, 
                runnableState, 
                cfg.Tags.ToArray(),
                details);

            return runnableInfo;

        }).OrderBy(x => x.Id).ToArray();
        
        return new WorkspaceInfo(WorkspacePath, runnables, _configurator.Current.Flavours.ToArray(), CurrentFlavour, serverState, state);
    }

    private async Task AddAsync(string id, IRunnableConfig cfg, TimeSpan delay, CancellationToken token)
    {
        if (_runnables.ContainsKey(id)) return;
        var container = await RunnableContainer.CreateAsync(cfg, _createRunnable, delay, token);

        container.Runnable.OnHealthCheckCompleted += OnPublishStatus;
        container.Runnable.OnInitExecuted += OnRunnableInitExecuted;
            
        _runnables.TryAdd(id, container);

        container.Start();
    }

    private async Task<bool> RemoveAsync(string key)
    {
        if (!_runnables.TryRemove(key, out var container)) return false;

        Interlocked.Decrement(ref _initCounter);
        container.Runnable.OnHealthCheckCompleted -= OnPublishStatus;
        container.Runnable.OnInitExecuted -= OnRunnableInitExecuted;
        await container.CancelAsync();
        container.Dispose();
        return true;
    }

    private void OnRunnableInitExecuted(object? sender, EventArgs e)
    {
        if (_configurator.Current.Count != Interlocked.Increment(ref _initCounter)) return;
        using var _ = _logger.WithHostScope(Phase.INIT);
        OnInitiated?.Invoke(this, EventArgs.Empty);
    }
    private void OnPublishStatus(object? sender, EventArgs args) => PublishStatusCore(ServerState.RUNNING, force: false);

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
        _sender.Enqueue(Message.WorkspaceInfo(CurrentInfo));
    }

    public void Dispose() => _cts.Dispose();

    public async Task WaitUntilStoppedAsync(CancellationToken token)
    {
        if (_stopTask != null) await _stopTask;
    }
}
