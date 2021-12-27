using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions;
using ATech.Ring.DotNet.Cli.CsProj;
using ATech.Ring.DotNet.Cli.Dtos;
using ATech.Ring.DotNet.Cli.Runnables;
using ATech.Ring.DotNet.Cli.Windows.Tools;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using Microsoft.Extensions.Logging;
using IISExpressConfig = ATech.Ring.Configuration.Runnables.IISExpress;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.IISExpress
{
    public class IISExpressRunnable : CsProjRunnable<IISExpressContext, IISExpressConfig>
    {
        private readonly IISExpressExe _iisExpress;
        private readonly ILogger<IISExpressRunnable> _logger;
        private readonly Func<Uri, HttpClient> _clientFactory;
        private readonly List<string> _wcfServices = new();
        public IISExpressRunnable(IISExpressConfig config,
                                  IISExpressExe iisExpress,
                                  ILogger<IISExpressRunnable> logger,
                                  ISender<IRingEvent> eventQ,
                                  Func<Uri, HttpClient> clientFactory) : base(config, logger, eventQ)
        {
            _iisExpress = iisExpress;
            _logger = logger;
            _clientFactory = clientFactory;
        }

        protected override IISExpressContext CreateContext()
        {
            AddDetail(DetailsKeys.CsProjPath, Config.FullPath);
            var uri = Config.GetIISUrl();

            var ctx = new IISExpressContext
            {
                CsProjPath = Config.CsProj,
                WorkingDir = Config.GetWorkingDir(),
                EntryAssemblyPath = $"{Config.GetWorkingDir()}\\bin\\{Config.GetProjName()}.dll",
                Uri = uri
            };

            AddDetail(DetailsKeys.WorkDir, ctx.WorkingDir);
            AddDetail(DetailsKeys.ProcessId, ctx.ProcessId);
            AddDetail(DetailsKeys.Uri, ctx.Uri);

            return ctx;
        }

        protected override async Task<IISExpressContext> InitAsync(CancellationToken token)
        {
            var ctx = await base.InitAsync(token);
            _wcfServices.AddRange(new DirectoryInfo(ctx.WorkingDir).EnumerateFiles("*.svc", SearchOption.TopDirectoryOnly).Select(f => f.Name));
            var apphostConfig = new ApphostConfig { VirtualDir = ctx.WorkingDir, Uri = ctx.Uri };
            ctx.TempAppHostConfigPath = apphostConfig.Ensure();
            return ctx;
        }

        protected override async Task StartAsync(IISExpressContext ctx, CancellationToken token)
        {
            var result = await _iisExpress.StartWebsite(ctx.TempAppHostConfigPath);
            ctx.ProcessId = result.Pid;
            ctx.Output = result.Output;
            _logger.LogInformation("{Uri}", ctx.Uri);
        }

        protected override async Task<HealthStatus> CheckHealthAsync(IISExpressContext ctx, CancellationToken token)
        {
            var processCheck = await base.CheckHealthAsync(ctx, token);

            if (processCheck != HealthStatus.Ok) return processCheck;
            try
            {
                foreach (var s in _wcfServices)
                {
                    if (token.IsCancellationRequested) continue;
                    var client = _clientFactory(ctx.Uri);
                    using var rq = new HttpRequestMessage(HttpMethod.Get, s);
                    var response = await client.SendAsync(rq, HttpCompletionOption.ResponseHeadersRead, token);
                    var isHealthy = response != null && response.StatusCode == HttpStatusCode.OK;
                    if (isHealthy) continue;

                    _logger.LogError("Endpoint {ServiceName} failed.", s);
                    return HealthStatus.Unhealthy;
                }

                return HealthStatus.Ok;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "HealthCheck Failed {UniqueId}", UniqueId);
                return HealthStatus.Unhealthy;
            }
        }
    }
}

