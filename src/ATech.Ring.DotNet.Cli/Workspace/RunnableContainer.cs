using System;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions;

namespace ATech.Ring.DotNet.Cli.Workspace;

internal sealed class RunnableContainer : IDisposable
{
    private readonly CancellationTokenSource _aggregateCts;
    private readonly CancellationTokenSource _cts = new();
    public IRunnable Runnable { get; private set; }
    public Task? Task { get; private set; }

    private RunnableContainer(IRunnable runnable, CancellationToken token) 
    {
        Runnable = runnable;
        _aggregateCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, token);
    }

    private async Task InitialiseAsync(TimeSpan delay)
    {
        if (delay != TimeSpan.Zero) await Task.Delay(delay, _aggregateCts.Token);
    }

    public static async Task<RunnableContainer> CreateAsync(IRunnableConfig cfg, Func<IRunnableConfig, IRunnable> factory, TimeSpan delay, CancellationToken token)
    {
        var container = new RunnableContainer(factory(cfg), token);
        await container.InitialiseAsync(delay);
        return container;
    }
        
    public void Start() => Task = Runnable.RunAsync(_aggregateCts.Token);

    public async Task CancelAsync()
    {
        _cts.Cancel();
        if (Task is { } t) await t;
        if (Runnable is { } r) await r.TerminateAsync();
    }

    public void Dispose()
    {
        _aggregateCts?.Dispose();
        _cts?.Dispose();
    }
}