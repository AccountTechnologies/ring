using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.CsProj;
using ATech.Ring.DotNet.Cli.Tools;
using ATech.Ring.Protocol.v2;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Runnables.Dotnet
{
    public abstract class DotnetRunnableBase<TContext, TConfig> : ProcessRunnable<TContext, TConfig>
        where TContext : DotnetContext
        where TConfig : IUseCsProjFile, IRunnableConfig
    {
        protected readonly DotnetCliBundle Dotnet;
        private readonly ILogger<DotnetRunnableBase<TContext, TConfig>> _logger;
        private readonly GitClone _gitClone;

        protected DotnetRunnableBase(TConfig config,
                                     DotnetCliBundle dotnet,
                                     ILogger<DotnetRunnableBase<TContext, TConfig>> logger, 
                                     ISender sender,
                                     GitClone gitClone
                                     ) : base(config, logger, sender)
        {
            Dotnet = dotnet;
            _logger = logger;
            _gitClone = gitClone;
        }

        public override string UniqueId => Config.GetProjName();

        protected override async Task<TContext> InitAsync(CancellationToken token)
        {
            if (Config is IFromGit { SshRepoUrl: string _ } gitCfg) await _gitClone.CloneOrPullAsync(gitCfg, token, shallow: true, defaultBranchOnly: true);
 
            var ctx = DotnetContext.Create<TContext,TConfig>(Config, c => _gitClone.ResolveFullClonePath(c));
            if (File.Exists(ctx.EntryAssemblyPath)) return ctx;

            _logger.LogDebug("Building {Project}", ctx.CsProjPath);
            var result = await Dotnet.BuildAsync(ctx.CsProjPath, token);

            if (!result.IsSuccess)
            {
                _logger.LogInformation("Build failed | {output}", result.Output);
            }
            return ctx;
        }

        protected override async Task StartAsync(TContext ctx, CancellationToken token)
        {
            var info = await Dotnet.RunAsync(ctx, token);
            ctx.ProcessId = info.Pid;
            ctx.Output = info.Output;
        }
    }
}
