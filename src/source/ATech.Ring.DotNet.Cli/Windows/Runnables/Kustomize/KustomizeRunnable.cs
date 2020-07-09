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

    protected override async Task<KustomizeContext> InitAsync(CancellationToken token)
    {
      var kustomizationDir = Config.IsRemote() ? Config.Path : Config.FullPath;
      AddDetail(DetailsKeys.KustomizationDir, kustomizationDir);
      var ctx = new KustomizeContext { KustomizationDir = kustomizationDir };

      var namespacesPath = "{range .items[?(@.kind=='Namespace')]}{.metadata.name}{'\\n'}{end}";

      var result = await _kubectl.ApplyKJsonPathAsync(ctx.KustomizationDir, namespacesPath);
      var namespaces = result.Output.Split("\n");
      _logger.LogDebug(result.Output);

      var results =
        await Task.WhenAll(
          namespaces.Select(async ns =>
          {
            var infos = await _kubectl.GetResources("pod", ns);
            _logger.LogDebug(infos.Output);
            return infos.Output.Split("\n").Select(podName => new PodInfo(ns, podName));

          }).ToArray());

      ctx.Pods = results.SelectMany(pods => pods).ToArray();

      return ctx;
    }

    protected override Task StartAsync(KustomizeContext ctx, CancellationToken token) => Task.CompletedTask;

    protected override async Task<HealthStatus> CheckHealthAsync(KustomizeContext ctx, CancellationToken token)
    {
      var podsHealthy = await Task.WhenAll(
        ctx.Pods.Select(async p => {
          var result = await _kubectl.GetPodStatus(p.PodName, p.Ns);
          return result.Output == "Running";
          }));
      return podsHealthy.All(x => x) ? HealthStatus.Ok : HealthStatus.Unhealthy;
    }

    protected override Task StopAsync(KustomizeContext ctx, CancellationToken token) => Task.CompletedTask;

    protected override async Task DestroyAsync(KustomizeContext ctx, CancellationToken token)
    {
      await _kubectl.DeleteKAsync(ctx.KustomizationDir);
    }
  }
}
