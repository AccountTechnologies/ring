using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions;
using ATech.Ring.DotNet.Cli.Dtos;
using ATech.Ring.DotNet.Cli.Infrastructure;
using ATech.Ring.DotNet.Cli.Windows.Tools;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KustomizeConfig = ATech.Ring.Configuration.Runnables.Kustomize;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.Kustomize
{
    public class KustomizeRunnable : Runnable<KustomizeContext, KustomizeConfig>
    {
        private const string NamespacesPath = "{range .items[?(@.kind=='Namespace')]}{.metadata.name}{'\\n'}{end}";
        private const string PodStatusRunning = "Running";
        private readonly string _cacheDir;
        private readonly ILogger<Runnable<KustomizeContext, KustomizeConfig>> _logger;
        private readonly Tools.KubectlBundle _bundle;

        public KustomizeRunnable(
            KustomizeConfig config,
            IOptions<RingConfiguration> ringCfg,
            ILogger<Runnable<KustomizeContext, KustomizeConfig>> logger,
            ISender<IRingEvent> sender,
            KubectlBundle bundle) : base(config, logger, sender)
        {
            _logger = logger;
            _bundle = bundle;
            _cacheDir = ringCfg?.Value?.KustomizeCacheRootPath ?? throw new ArgumentNullException(nameof(RingConfiguration.KustomizeCacheRootPath));
        }

        public override string UniqueId => Config.Path;
        protected override TimeSpan HealthCheckPeriod => TimeSpan.FromSeconds(10);
        protected override int MaxTotalFailuresUntilDead => 10;
        protected override int MaxConsecutiveFailuresUntilDead => 5;

        private string GetCachePath(string inputDir) =>  $"{_cacheDir}/{Regex.Replace(inputDir, "[@\\.:/\\\\]", "-")}.yaml";

        protected override async Task<KustomizeContext> InitAsync(CancellationToken token)
        {
            var kustomizationDir = Config.IsRemote() ? Config.Path : Config.FullPath;
            AddDetail(DetailsKeys.KustomizationDir, kustomizationDir);
            var ctx = new KustomizeContext
            {
                KustomizationDir = kustomizationDir,
                CachePath = GetCachePath(kustomizationDir)
            };

            await _bundle.RunProcessWaitAsync("mkdir", "-p", _cacheDir);

            var kustomizeResult = await _bundle.KustomizeBuildAsync(kustomizationDir, ctx.CachePath);
            _logger.LogDebug(kustomizeResult.Output);

            var applyResult = await _bundle.ApplyJsonPathAsync(ctx.CachePath, NamespacesPath);

            _logger.LogDebug(applyResult.Output);
            var namespaces = applyResult.Output.Split(Environment.NewLine);

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
                      var infos = await _bundle.GetResources("pod", n.Name);
                      _logger.LogDebug("Pods: {pods}", infos.Output);
                      return infos.Output.Split(Environment.NewLine);
                  }
                  var podsNow = await GetPodsAsync();
                  if (n.Pods.Any() && !podsNow.Any()) return false;
                  n.Pods = podsNow;

                  return (await Task.WhenAll(n.Pods.Select(async p =>
                  {
                      var result = await _bundle.GetPodStatus(p, n.Name);
                      return result.Output == PodStatusRunning;
                  }))).All(x => x);
              }));
            return podsHealthy.All(x => x) ? HealthStatus.Ok : HealthStatus.Unhealthy;
        }

        protected override Task StopAsync(KustomizeContext ctx, CancellationToken token) => Task.CompletedTask;
        protected override async Task DestroyAsync(KustomizeContext ctx, CancellationToken token) => await _bundle.DeleteAsync(ctx.CachePath);
    }
}
