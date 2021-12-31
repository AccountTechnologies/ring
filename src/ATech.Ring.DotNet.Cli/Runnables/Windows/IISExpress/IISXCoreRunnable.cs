using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Dtos;
using ATech.Ring.DotNet.Cli.Runnables;
using ATech.Ring.DotNet.Cli.Tools;
using ATech.Ring.DotNet.Cli.Windows.Tools;
using ATech.Ring.Protocol.v2;
using ATech.Ring.Protocol.v2.Events;
using Microsoft.Extensions.Logging;
using IISXCoreConfig =  ATech.Ring.Configuration.Runnables.IISXCore;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.IISExpress
{
    public class IISXCoreRunnable : CsProjRunnable<IISXCoreContext, IISXCoreConfig>
    {
        private readonly IISExpressExe _iisExpress;
        private readonly ILogger<IISXCoreRunnable> _logger;
        private readonly GitClone _gitClone;

        public IISXCoreRunnable(
            IISXCoreConfig config,
            IISExpressExe iisExpress,
            ILogger<IISXCoreRunnable> logger,
            ISender<IRingEvent> eventQ, GitClone gitClone) : base(config, logger, eventQ)
        {
            _iisExpress = iisExpress;
            _logger = logger;
            _gitClone = gitClone;
        }

        protected override IISXCoreContext CreateContext()
        {
            AddDetail(DetailsKeys.CsProjPath, Config.FullPath);
            var ctx = IISXCoreContext.Create(Config, c => _gitClone.ResolveFullClonePath(c));
            AddDetail(DetailsKeys.WorkDir, ctx.WorkingDir);
            AddDetail(DetailsKeys.ProcessId, ctx.ProcessId);
            AddDetail(DetailsKeys.Uri, ctx.Uri);
            return ctx;
        }

        protected override async Task<IISXCoreContext> InitAsync(CancellationToken token)
        {
            var ctx = await base.InitAsync(token);
            var apphostConfig = new ApphostConfig { VirtualDir = ctx.WorkingDir, Uri = ctx.Uri };
            ctx.TempAppHostConfigPath = apphostConfig.Ensure();
            return ctx;
        }

        protected override async Task StartAsync(IISXCoreContext ctx, CancellationToken token)
        {
            var result = await _iisExpress.StartWebsite(ctx.TempAppHostConfigPath, new Dictionary<string, string>
            {
                ["LAUNCHER_PATH"] = ctx.ExePath
            });
            ctx.ProcessId = result.Pid;
            ctx.Output = result.Output;
            _logger.LogInformation("{Uri}", ctx.Uri);
        }
    }
}