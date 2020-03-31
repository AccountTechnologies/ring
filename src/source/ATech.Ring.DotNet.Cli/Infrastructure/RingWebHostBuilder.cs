using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
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
        private static readonly IServiceContainer Container = new ServiceContainer(new ContainerOptions { EnablePropertyInjection = false, EnableVariance = false });

        public static IWebHostBuilder ForRing(this IWebHostBuilder hostBuilder, string[] args)
        {
            Container.ScopeManagerProvider = new PerLogicalCallContextScopeManagerProvider();
            BaseOptions options = new ConsoleOptions { IsDebug = false };
            Parser.Default.ParseArguments<ConsoleOptions, HeadlessOptions>(args)
                          .WithParsed<ConsoleOptions>(opts => 
                          {
                              opts.WorkspacePath = Path.GetFullPath(opts.WorkspacePath, Startup.OriginalWorkingDir);
                              options = opts;
                          })
                          .WithParsed<HeadlessOptions>(opts => options = opts)
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
            var wshTask = w.Services.GetRequiredService<WebsocketsHandler>().InitializeAsync(w.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);
            await await Task.WhenAny(wshTask, runTask);
        }
    }
}