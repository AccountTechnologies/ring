using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Infrastructure.Cli;
using ATech.Ring.DotNet.Cli.Workspace;
using LightInject;
using LightInject.Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    public static class RingWebHostBuilder
    {
        private static string Ring(string ver) => $"       _ _\n     *'   '*\n    * .*'*. 3\n   '  @   a  ;     ring! v{ver}\n    * '*.*' *\n     *. _ .*\n";
        private static readonly IServiceContainer Container = new ServiceContainer(new ContainerOptions { EnablePropertyInjection = false, EnableVariance = false });

        public static IWebHostBuilder ForRing(this IWebHostBuilder hostBuilder, string[] args)
        {
            Container.ScopeManagerProvider = new PerLogicalCallContextScopeManagerProvider();

            var options = CliParser.GetOptions(args);

            if (!options.NoLogo)
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                Console.WriteLine(Ring(version.ToString()));
            }

            hostBuilder
                 .UseStartup<Startup>()
                 .UseConfiguration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddInMemoryCollection(new Dictionary<string, string> { ["ring:port"] = options.Port.ToString() })
                    .AddUserSettingsFile()
                    .AddEnvironmentVariables()
                    .Build())
                 .UseKestrel((ctx, opts) =>
                 {
                     opts.ListenAnyIP(ctx.Configuration.GetValue<int>("ring:port"));
                 })
                 .SuppressStatusMessages(true)
                 .ConfigureServices(s => s.AddSingleton(f => options))
                 .UseLightInject(Container);

            return hostBuilder;
        }

        public static async Task RunRingAsync(this IWebHost host)
        {
            var opts = host.Services.GetRequiredService<BaseOptions>();
            if (opts is CloneOptions c)
            {
                await host.Services.GetRequiredService<ICloneMaker>().CloneWorkspaceRepos(c.WorkspacePath, c.OutputDir);
            }
            else
            {
                await host.RunAsync();
            }
        }
    }
}
