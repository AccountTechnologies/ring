using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions;
using ATech.Ring.DotNet.Cli.Dtos;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.Kustomize
{
    public class KustomizeRunnable : Runnable<KustomizeContext, Configuration.Runnables.Kustomize>
    {
        private readonly Tools.Kubectl _kubectl;

        public KustomizeRunnable(ILogger<Runnable<KustomizeContext, Configuration.Runnables.Kustomize>> logger,
            ISender<IRingEvent> sender,
            Tools.Kubectl dockerCompose) : base(logger, sender)
        {
            _kubectl = dockerCompose;
        }

        public override string UniqueId => Config.Path;

        protected override async Task<KustomizeContext> InitAsync(CancellationToken token)
        {
            AddDetail(DetailsKeys.KustomizationDir, Config.FullPath);
            var ctx = new KustomizeContext{KustomizationDir = Config.FullPath };
            await _kubectl.ApplyKAsync(ctx.KustomizationDir);
            return ctx;
        }

        protected override Task StartAsync(KustomizeContext ctx, CancellationToken token) => Task.CompletedTask;

        protected override Task<HealthStatus> CheckHealthAsync(KustomizeContext ctx, CancellationToken token)
        {
            return Task.FromResult(HealthStatus.Ok);
        }

        protected override Task StopAsync(KustomizeContext ctx, CancellationToken token) => Task.CompletedTask;

        protected override async Task DestroyAsync(KustomizeContext ctx, CancellationToken token)
        { 
            await _kubectl.DeleteKAsync(ctx.KustomizationDir);
        }
    }
}
