using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.CsProj;
using ATech.Ring.DotNet.Cli.Windows.Tools;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.Dotnet
{
    public abstract class DotnetRunnableBase<TContext, TConfig> : WindowsRunnable<TContext, TConfig>
        where TContext : DotnetContext
        where TConfig : IUseCsProjFile, IRunnableConfig
    {
        protected readonly DotnetCliBundle Dotnet;
        private readonly ILogger<DotnetRunnableBase<TContext, TConfig>> _logger;
        private readonly GitClone _gitClone;

        protected DotnetRunnableBase(TConfig config,
                                     DotnetCliBundle dotnet,
                                     ILogger<DotnetRunnableBase<TContext, TConfig>> logger, 
                                     ISender<IRingEvent> eventQ,
                                     GitClone gitClone
                                     ) : base(config, logger, eventQ)
        {
            Dotnet = dotnet;
            _logger = logger;
            _gitClone = gitClone;
        }

        public override string UniqueId => Config.GetProjName();

        protected override async Task<TContext> InitAsync(CancellationToken token)
        {
            if (Config is IFromGit { SshRepoUrl: string _ } gitCfg) await _gitClone.CloneOrPullAsync(gitCfg, token, shallow: true, masterOnly: true);
 
            var ctx = DotnetContext.Create<TContext,TConfig>(Config, c => _gitClone.ResolveFullClonePath(c));
            if (File.Exists(ctx.EntryAssemblyPath)) return ctx;

            _logger.LogDebug("Building {Project}", ctx.CsProjPath);
            var result = await Dotnet.BuildAsync(ctx.CsProjPath);

            if (!result.IsSuccess)
            {
                _logger.LogInformation($"Build failed | {result.Output}");
            }
            return ctx;
        }

        protected override async Task StartAsync(TContext ctx, CancellationToken token)
        {
            var info = await Dotnet.RunAsync(ctx);
            ctx.ProcessId = info.Pid;
            ctx.Output = info.Output;
        }
    }
}