using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions.Context;
using ATech.Ring.DotNet.Cli.CsProj;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables
{
    public abstract class CsProjRunnable<TContext, TConfig> : ProcessRunnable<TContext, TConfig>
        where TContext : ITrackProcessId, ICsProjContext, ITrackRetries
        where TConfig : IRunnableConfig, IUseCsProjFile
    {
        protected CsProjRunnable(TConfig config, ILogger<CsProjRunnable<TContext, TConfig>> logger, ISender<IRingEvent> eventQ) : base(config, logger, eventQ)
        {
        }

        protected abstract TContext CreateContext();
        protected override Task<TContext> InitAsync(CancellationToken token) => Task.FromResult(CreateContext());
        public override string UniqueId => Config.GetProjName();
    }
}
