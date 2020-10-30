using System;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions;

namespace ATech.Ring.DotNet.Cli.Workspace
{
    internal class RunnableContainer : IDisposable
    {
        private CancellationTokenSource _aggregateCts;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private RunnableContainer() { }
        public IRunnable Runnable { get; private set; }
        public Task Task { get; private set; }

        private void Initialise(IRunnable runnable, CancellationToken token)
        {
            _aggregateCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, token);
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
            if (delay != TimeSpan.Zero) await Task.Delay(delay, token);
            container.Initialise(runnable, token);
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
