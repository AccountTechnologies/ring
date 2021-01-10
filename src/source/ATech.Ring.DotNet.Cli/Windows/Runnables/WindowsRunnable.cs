using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions;
using ATech.Ring.DotNet.Cli.Abstractions.Context;
using ATech.Ring.DotNet.Cli.Windows.Tools;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables
{
    public abstract class WindowsRunnable<TContext, TConfig> : Runnable<TContext, TConfig>
        where TContext : ITrackProcessId
        where TConfig : IRunnableConfig
    {
        protected WindowsRunnable(TConfig config, ILogger<WindowsRunnable<TContext, TConfig>> logger, ISender<IRingEvent> eventQ) : base(config, logger, eventQ)
        {
        }

        protected override Task DestroyAsync(TContext ctx, CancellationToken token) => Task.CompletedTask;

        /// <summary>
        /// The default implementation checks whether the process exists
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override Task<HealthStatus> CheckHealthAsync(TContext ctx, CancellationToken token)
        {
            return ctx.ProcessId == 0 ? 
                   Task.FromResult(HealthStatus.Unhealthy) 
                                      : Task.FromResult(ProcessExtensions.TryGetProcessById(ctx.ProcessId) ? HealthStatus.Ok : HealthStatus.Unhealthy);
        }

        /// <summary>
        /// The default implementation kills the process
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override Task StopAsync(TContext ctx, CancellationToken token)
        {
            if (ctx == null || ctx.ProcessId == 0) return Task.CompletedTask;
            ProcessExtensions.KillProcess(ctx.ProcessId);
            return Task.CompletedTask;
        }
    }
}
