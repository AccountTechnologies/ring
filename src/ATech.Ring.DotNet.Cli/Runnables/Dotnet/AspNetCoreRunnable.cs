using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Runnables;
using ATech.Ring.DotNet.Cli.Tools;
using ATech.Ring.Protocol.v2;
using Microsoft.Extensions.Logging;
using static ATech.Ring.DotNet.Cli.Dtos.DetailsKeys;

namespace ATech.Ring.DotNet.Cli.Runnables.Dotnet;

public class AspNetCoreRunnable : DotnetRunnableBase<AspNetCoreContext, AspNetCore>
{

    public AspNetCoreRunnable(AspNetCore config, DotnetCliBundle dotnet, ILogger<AspNetCoreRunnable> logger, ISender sender, GitClone gitClone) : base(config, dotnet, logger, sender, gitClone)
    {
    }

    protected override async Task<AspNetCoreContext> InitAsync(CancellationToken token)
    {
        AddDetail(CsProjPath, Config.FullPath);
        var ctx = await base.InitAsync(token);
        ctx.Urls = Config.Urls;
        AddDetail(WorkDir, ctx.WorkingDir);
        AddDetail(ProcessId, ctx.ProcessId);
        if (ctx.Urls.Any()) AddDetail(Uri, ctx.Urls);
        return ctx;
    }

    protected override async Task StartAsync(AspNetCoreContext ctx, CancellationToken token)
    {
        var info = await Dotnet.RunAsync(ctx, token, ctx.Urls);
        ctx.ProcessId = info.Pid;
        ctx.Output = info.Output;
    }
}