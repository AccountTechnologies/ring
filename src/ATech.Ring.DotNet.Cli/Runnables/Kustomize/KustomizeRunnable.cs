﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions;
using ATech.Ring.DotNet.Cli.Dtos;
using ATech.Ring.DotNet.Cli.Infrastructure;
using ATech.Ring.DotNet.Cli.Tools;
using ATech.Ring.Protocol.v2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static ATech.Ring.DotNet.Cli.Tools.ToolExtensions;
using KustomizeConfig = ATech.Ring.Configuration.Runnables.Kustomize;

namespace ATech.Ring.DotNet.Cli.Runnables.Kustomize;

public class KustomizeRunnable : Runnable<KustomizeContext, KustomizeConfig>
{
    private static class PodStatus
    {
        public const string Running = "Running";
        public const string Error = "Error";
    }
    private const string NamespacesPath = "{range .items[?(@.kind=='Namespace')]}{.metadata.name}{'\\n'}{end}";
    private readonly string _cacheDir;
    private readonly ILogger<Runnable<KustomizeContext, KustomizeConfig>> _logger;
    private readonly KubectlBundle _bundle;

    public KustomizeRunnable(
        KustomizeConfig config,
        IOptions<RingConfiguration> ringCfg,
        ILogger<Runnable<KustomizeContext, KustomizeConfig>> logger,
        ISender sender,
        KubectlBundle bundle) : base(config, logger, sender)
    {
        _logger = logger;
        _bundle = bundle;
        _cacheDir = ringCfg?.Value?.Kustomize.CachePath ?? throw new ArgumentNullException(nameof(RingConfiguration.Kustomize.CachePath));
    }

    public override string UniqueId => Config.Path;
    protected override TimeSpan HealthCheckPeriod => TimeSpan.FromSeconds(10);
    protected override int MaxTotalFailuresUntilDead => 10;
    protected override int MaxConsecutiveFailuresUntilDead => 5;

    private string GetCachePath(string inputDir)
    {
        var fileName = Regex.Replace(inputDir, "[@\\.:/\\\\]", "-");
        return Path.Combine(_cacheDir, $"{fileName}.yaml");
    }

    private async Task<bool> WaitAllPodsAsync(KustomizeContext ctx, CancellationToken token, params string[] statuses) =>
        (await Task.WhenAll(
            ctx.Namespaces.Select(async n =>
            {
                async Task<string[]> GetPodsAsync()
                {
                    var pods = await _bundle.GetPods(n.Name);
                    _logger.LogDebug("Pods: {pods}", new object[] { pods });
                    return pods;
                }
                var podsNow = await GetPodsAsync();
                if (n.Pods.Any() && !podsNow.Any()) return false;
                n.Pods = podsNow;

                return (await Task.WhenAll(n.Pods.Select(async p =>
                {
                    try
                    {
                        var result = await _bundle.GetPodStatus(p, n.Name, token);
                        return statuses.Contains(result);
                    }
                    catch (OperationCanceledException)
                    {
                        return false;
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "Could not get pod status");
                        return false;
                    }
                }))).All(x => x);
            }))).All(x => x);

    protected override async Task<KustomizeContext> InitAsync(CancellationToken token)
    {
        var kustomizationDir = Config.IsRemote() ? Config.Path : Config.FullPath;
        AddDetail(DetailsKeys.KustomizationDir, kustomizationDir);
        var ctx = new KustomizeContext
        {
            KustomizationDir = kustomizationDir,
            CachePath = GetCachePath(kustomizationDir)
        };
        Directory.CreateDirectory(_cacheDir);
        
        if (!File.Exists(ctx.CachePath) || !await _bundle.IsValidManifestAsync(ctx.CachePath, token))
        {
            var kustomizeResult = await _bundle.KustomizeBuildAsync(kustomizationDir, ctx.CachePath, token);
            _logger.LogDebug(kustomizeResult.Output);
        }
        else
        {
            _logger.LogInformation("Found cached manifest: {CachedManifestPath}", ctx.CachePath);
        }

        var applyResult = await _bundle.TryAsync(10, TimeSpan.FromSeconds(2),
            async t => await _bundle.ApplyJsonPathAsync(ctx.CachePath, NamespacesPath, token), token);

        _logger.LogDebug(applyResult.Output);

        if (!applyResult.IsSuccess)
        {
            throw new InvalidOperationException("Could not apply manifest");
        }
        var namespaces = applyResult.Output.Split(Environment.NewLine);

        ctx.Namespaces = namespaces.Select(n => new Namespace { Name = n }).ToArray();

        return ctx;
    }

    protected override async Task StartAsync(KustomizeContext ctx, CancellationToken token)
    {
        await TryAsync(100, TimeSpan.FromSeconds(6),
            async () => await WaitAllPodsAsync(ctx, token, PodStatus.Running, PodStatus.Error), r => r, token);
        AddDetail(DetailsKeys.Pods, string.Join("|", ctx.Namespaces.SelectMany(n => n.Pods.Select(p => n.Name + "/" + p))));
    }

    protected override async Task<HealthStatus> CheckHealthAsync(KustomizeContext ctx, CancellationToken token)
    {
        if (ctx.Namespaces == null) return HealthStatus.Dead;
        var podsHealthy = await WaitAllPodsAsync(ctx, token, PodStatus.Running);
        return podsHealthy ? HealthStatus.Ok : HealthStatus.Unhealthy;
    }

    protected override Task StopAsync(KustomizeContext ctx, CancellationToken token) => Task.CompletedTask;
    protected override async Task DestroyAsync(KustomizeContext ctx, CancellationToken token) => await _bundle.DeleteAsync(ctx.CachePath, token);
}