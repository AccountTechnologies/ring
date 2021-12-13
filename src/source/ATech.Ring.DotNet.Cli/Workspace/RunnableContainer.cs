using System;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions;

namespace ATech.Ring.DotNet.Cli.Workspace
{
    internal sealed class RunnableContainer : IDisposable
    {
        private CancellationTokenSource _aggregateCts;
        private readonly CancellationTokenSource _cts = new();
        private RunnableContainer() { }
        public IRunnable Runnable { get; private set; }
        public Task Task { get; private set; }

        private async Task InitialiseAsync(IRunnable runnable, TimeSpan delay, CancellationToken token)
        {
            _aggregateCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, token);
            if (delay != TimeSpan.Zero) await Task.Delay(delay, _aggregateCts.Token);
            Runnable = runnable;
        }

        public async Task CancelAsync()
        {
            _cts.Cancel();
            await Task;
            await Runnable.TerminateAsync();
        }

        public static async Task<RunnableContainer> CreateAsync(IRunnableConfig cfg, Func<IRunnableConfig, IRunnable> factory, TimeSpan delay, CancellationToken token)
        {
            var runnable = factory(cfg);
            var container = new RunnableContainer();
            await container.InitialiseAsync(runnable, delay, token);
            return container;
        }
        
        public void Start() => Task = Runnable.RunAsync(_aggregateCts.Token);

        public void Dispose()
        {
            _aggregateCts?.Dispose();
            _cts?.Dispose();
        }
    }
}
