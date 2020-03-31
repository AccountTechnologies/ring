using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Runnables;
using ATech.Ring.DotNet.Cli.Windows.Tools;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using Microsoft.Extensions.Logging;
using static ATech.Ring.DotNet.Cli.Dtos.DetailsKeys;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.Dotnet
{
    public class AspNetCoreRunnable : DotnetRunnableBase<AspNetCoreContext, AspNetCore>
    {

        public AspNetCoreRunnable(DotnetCliBundle dotnet, ILogger<AspNetCoreRunnable> logger, ISender<IRingEvent> sender) : base(dotnet, logger, sender)
        {
        }

        protected override async Task<AspNetCoreContext> InitAsync(CancellationToken token)
        {
            AddDetail(CsProjPath, Config.FullPath);
            var ctx = await base.InitAsync(token);
            ctx.Urls = Config.Urls;
            AddDetail(WorkDir, ctx.WorkingDir);
            AddDetail(ProcessId, ctx.ProcessId);
            AddDetail(Uri, ctx.Urls);
            return ctx;
        }

        protected override async Task StartAsync(AspNetCoreContext ctx, CancellationToken token)
        {
            var info = await Dotnet.RunAsync(ctx, ctx.Urls);
            ctx.ProcessId = info.Pid;
            ctx.Output = info.Output;
        }
    }
}