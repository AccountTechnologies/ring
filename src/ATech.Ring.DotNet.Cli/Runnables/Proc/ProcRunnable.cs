using ATech.Ring.DotNet.Cli.Abstractions.Context;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using ATech.Ring.Protocol.v2;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;

namespace ATech.Ring.DotNet.Cli.Runnables.Proc;

public class ProcContext : ITrackProcessId
{
    public int ProcessId { get; set; }
}

public class ProcRunnable : ProcessRunnable<ProcContext, Configuration.Runnables.Proc>
{
    private readonly ITool _tool;

    public ProcRunnable(Configuration.Runnables.Proc config,
        ILogger<ProcessRunnable<ProcContext, Configuration.Runnables.Proc>> logger, 
        ISender sender,
        ITool tool) : base(config, logger, sender)
    {
        _tool = tool;
    }

    public override string UniqueId => Config.Id;

    protected override Task<ProcContext> InitAsync(CancellationToken token)
    {
        throw new System.NotImplementedException();
    }

    protected override Task StartAsync(ProcContext ctx, CancellationToken token)
    {
        //_tool.RunProcessAsync()
        return Task.CompletedTask;
    }
}