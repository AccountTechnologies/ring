using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.CsProj;
using ATech.Ring.DotNet.Cli.Dtos;
using ATech.Ring.DotNet.Cli.Tools;
using ATech.Ring.Protocol.v2;
using Microsoft.Extensions.Logging;
using NetExeConfig = ATech.Ring.Configuration.Runnables.NetExe;

namespace ATech.Ring.DotNet.Cli.Runnables.Windows.NetExe;

public class NetExeRunnable : CsProjRunnable<NetExeContext, NetExeConfig>
{
    private readonly ProcessRunner _processRunner;

    public NetExeRunnable(
        NetExeConfig config,
        ProcessRunner processRunner, 
        ILogger<NetExeRunnable> logger, 
        ISender sender) : base(config, logger, sender)
    {
        _processRunner = processRunner;
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
        _processRunner.Command = ctx.EntryAssemblyPath;
        var result = await _processRunner.RunProcessAsync(Config.Args, token);
        ctx.ProcessId = result.Pid;
        ctx.Output = result.Output;
    }
}
