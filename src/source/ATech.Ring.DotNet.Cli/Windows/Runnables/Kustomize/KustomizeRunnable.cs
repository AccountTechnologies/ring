using System;
using System.Linq;
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
        private const string NamespacesPath = "{range .items[?(@.kind=='Namespace')]}{.metadata.name}{'\\n'}{end}";
        private const string PodStatusRunning = "Running";
        private readonly ILogger<Runnable<KustomizeContext, Configuration.Runnables.Kustomize>> _logger;
        private readonly Tools.KubectlBundle _kubectl;

        public KustomizeRunnable(ILogger<Runnable<KustomizeContext, Configuration.Runnables.Kustomize>> logger,
            ISender<IRingEvent> sender,
            Tools.KubectlBundle kubectlBundle) : base(logger, sender)
        {
            _logger = logger;
            _kubectl = kubectlBundle;
        }

        public override string UniqueId => Config.Path;
        protected override TimeSpan HealthCheckPeriod => TimeSpan.FromSeconds(10);
        protected override int MaxTotalFailuresUntilDead => 10;
        protected override int MaxConsecutiveFailuresUntilDead => 5;

        protected override async Task<KustomizeContext> InitAsync(CancellationToken token)
        {
            var kustomizationDir = Config.IsRemote() ? Config.Path : Config.FullPath;
            AddDetail(DetailsKeys.KustomizationDir, kustomizationDir);
            var ctx = new KustomizeContext { KustomizationDir = kustomizationDir };

            

            var result = await _kubectl.ApplyKJsonPathAsync(ctx.KustomizationDir, NamespacesPath);

            _logger.LogDebug(result.Output);
            var namespaces = result.Output.Split("\n");

            ctx.Namespaces = namespaces.Select(n => new Namespace { Name = n }).ToArray();
            return ctx;
        }

        protected override Task StartAsync(KustomizeContext ctx, CancellationToken token) => Task.CompletedTask;

        protected override async Task<HealthStatus> CheckHealthAsync(KustomizeContext ctx, CancellationToken token)
        {
            var podsHealthy = await Task.WhenAll(
              ctx.Namespaces.Select(async n =>
              {

                  async Task<string[]> GetPodsAsync()
                  {
                      var infos = await _kubectl.GetResources("pod", n.Name);
                      _logger.LogDebug("Pods: {pods}", infos.Output);
                      return infos.Output.Split("\n");
                  }
                  var podsNow = await GetPodsAsync();
                  if (n.Pods.Any() && !podsNow.Any()) return false;
                  n.Pods = podsNow;

                  return (await Task.WhenAll(n.Pods.Select(async p =>
                  {
                      var result = await _kubectl.GetPodStatus(p, n.Name);
                      return result.Output == PodStatusRunning;
                  }))).All(x => x);
              }));
            return podsHealthy.All(x => x) ? HealthStatus.Ok : HealthStatus.Unhealthy;
        }

        protected override Task StopAsync(KustomizeContext ctx, CancellationToken token) => Task.CompletedTask;
        protected override async Task DestroyAsync(KustomizeContext ctx, CancellationToken token) => await _kubectl.DeleteKAsync(ctx.KustomizationDir);
    }
}
