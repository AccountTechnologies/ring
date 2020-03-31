using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.CsProj;
using ATech.Ring.DotNet.Cli.Dtos;
using ATech.Ring.DotNet.Cli.Windows.Tools;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.NetExe
{
    public class NetExeRunnable : CsProjRunnable<NetExeContext, Configuration.Runnables.NetExe>
    {
        private readonly ExeRunner _exeRunner;

        public NetExeRunnable(ExeRunner exeRunner, ILogger<NetExeRunnable> logger, ISender<IRingEvent> eventQ) : base(logger, eventQ)
        {
            _exeRunner = exeRunner;
        }

        protected override NetExeContext CreateContext()
        {
            AddDetail(DetailsKeys.CsProjPath, Config.FullPath);
            var ctx = new NetExeContext
            {
                CsProjPath = Config.CsProj,
                WorkingDir = Config.GetWorkingDir(),
                EntryAssemblyPath = $"{Config.GetWorkingDir()}\\bin\\Debug\\{Config.GetProjName()}.exe"
            };

            AddDetail(DetailsKeys.WorkDir, ctx.WorkingDir);
            AddDetail(DetailsKeys.ProcessId, ctx.ProcessId);

            return ctx;
        }

        protected override async Task StartAsync(NetExeContext ctx, CancellationToken token)
        {
            _exeRunner.ExePath = ctx.EntryAssemblyPath;
            var result = await _exeRunner.RunProcessAsync(Config.Args);
            ctx.ProcessId = result.Pid;
            ctx.Output = result.Output;
        }
    }
}
