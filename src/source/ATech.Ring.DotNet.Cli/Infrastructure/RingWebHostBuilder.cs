using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATech.Ring.Configuration;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Logging;
using ATech.Ring.DotNet.Cli.Windows.Tools;
using ATech.Ring.DotNet.Cli.Workspace;
using CommandLine;
using LightInject;
using LightInject.Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    public static class RingWebHostBuilder
    {
        private static readonly IServiceContainer Container = new ServiceContainer(new ContainerOptions { EnablePropertyInjection = false, EnableVariance = false });

        public static IWebHostBuilder ForRing(this IWebHostBuilder hostBuilder, string[] args)
        {
            Container.ScopeManagerProvider = new PerLogicalCallContextScopeManagerProvider();
            BaseOptions options = new ConsoleOptions { IsDebug = false };
            Parser.Default.ParseArguments<ConsoleOptions, HeadlessOptions, CloneOptions>(args)
                          .WithParsed<ConsoleOptions>(opts =>
                          {
                              opts.WorkspacePath = Path.GetFullPath(opts.WorkspacePath, Startup.OriginalWorkingDir);
                              options = opts;
                          })
                          .WithParsed<HeadlessOptions>(opts => options = opts)
                          .WithParsed<CloneOptions>(opts =>
                          {
                              opts.WorkspacePath = Path.GetFullPath(opts.WorkspacePath, Startup.OriginalWorkingDir);
                              options = opts;
                          })
                          .WithNotParsed(x => Environment.Exit(-1));

            hostBuilder
                 .UseStartup<Startup>()
                 .UseConfiguration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddInMemoryCollection(new Dictionary<string, string>() { ["ring:port"] = options.Port.ToString() })
                    .AddEnvironmentVariables().Build())
                 .UseKestrel((ctx, opts) =>
                 {
                     opts.ListenLocalhost(ctx.Configuration.GetValue<int>("ring:port"));
                 })
                 .SuppressStatusMessages(true)
                 .ConfigureServices(s => s.AddSingleton(f => options))
                 .UseLightInject(Container);

            return hostBuilder;
        }

        public static async Task RunRingAsync(this IWebHost w)
        {
            var runTask = w.RunAsync();
            var opts = w.Services.GetRequiredService<BaseOptions>();
            if (opts is CloneOptions c)
            {
                var configurator = w.Services.GetRequiredService<IConfigurator>();
                await configurator.LoadAsync(new ConfiguratorPaths { WorkspacePath = c.WorkspacePath },
                    w.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);
                var gitConfigs = configurator.Current.Values.OfType<IFromGit>();
                var gitTool = w.Services.GetRequiredService<GitClone>();

                foreach (var gitCfg in gitConfigs)
                {
                    var output = await gitTool.CloneOrPullAsync(gitCfg);
                    if (output.IsSuccess) continue;
                    break;
                }
                await w.StopAsync();
                return;
            }

            var wshTask = w.Services.GetRequiredService<WebsocketsHandler>().InitializeAsync(w.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);
            await await Task.WhenAny(wshTask, runTask);
        }
    }
}