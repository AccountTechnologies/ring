using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions;
using ATech.Ring.DotNet.Cli.Dtos;
using ATech.Ring.DotNet.Cli.Tools;
using ATech.Ring.Protocol.v2;
using Microsoft.Extensions.Logging;
using DockerComposeConfig = ATech.Ring.Configuration.Runnables.DockerCompose;

namespace ATech.Ring.DotNet.Cli.Runnables.DockerCompose;

public class DockerComposeRunnable : Runnable<DockerComposeContext, DockerComposeConfig>
{
    private readonly Tools.DockerCompose _dockerCompose;

    public DockerComposeRunnable(DockerComposeConfig config, ILogger<Runnable<DockerComposeContext, DockerComposeConfig>> logger,
        ISender sender,
        Tools.DockerCompose dockerCompose) : base(config, logger, sender)
    {
        _dockerCompose = dockerCompose;
    }

    public override string UniqueId => Config.Path;

    protected override async Task<DockerComposeContext> InitAsync(CancellationToken token)
    {
        AddDetail(DetailsKeys.DockerComposePath, Config.FullPath);
        var ctx = new DockerComposeContext{ComposeFilePath = Config.FullPath };
        await _dockerCompose.RmAsync(ctx.ComposeFilePath, token);
        await _dockerCompose.PullAsync(ctx.ComposeFilePath, token);
        return ctx;
    }

    protected override async Task StartAsync(DockerComposeContext ctx, CancellationToken token)
    {
        var result = await _dockerCompose.UpAsync(ctx.ComposeFilePath, token);
        ctx.ProcessId = result.Pid;
    }

    protected override Task<HealthStatus> CheckHealthAsync(DockerComposeContext ctx, CancellationToken token)
    {
        return Task.FromResult(ProcessExtensions.IsProcessRunning(ctx.ProcessId) ? HealthStatus.Ok : HealthStatus.Dead);
    }

    protected override async Task StopAsync(DockerComposeContext ctx, CancellationToken token)
    {
        await _dockerCompose.StopAsync(ctx.ComposeFilePath, token);
    }

    protected override async Task DestroyAsync(DockerComposeContext ctx, CancellationToken token)
    { 
        await _dockerCompose.DownAsync(ctx.ComposeFilePath, token);
    }
}