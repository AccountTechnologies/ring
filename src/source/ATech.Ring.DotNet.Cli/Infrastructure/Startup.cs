using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using ATech.Ring.Configuration;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using ATech.Ring.DotNet.Cli.Logging;
using ATech.Ring.DotNet.Cli.Workspace;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using LightInject;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nett;
using Serilog;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    public class Startup
    {
        public static readonly string OriginalWorkingDir = Directory.GetCurrentDirectory();
        public static readonly string RingBinPath = Path.GetDirectoryName(Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                                                           .Single(x => x.ModuleName == typeof(RingWebHostBuilder).Module.Name).FileName);

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var clients = new ConcurrentDictionary<Uri, HttpClient>();
            services.AddSingleton<Func<Uri, HttpClient>>(s => uri => clients.GetOrAdd(uri, new HttpClient {BaseAddress = uri, MaxResponseContentBufferSize = 1}));
            services.AddOptions();
            services.Configure<RingConfiguration>(Configuration.GetSection("ring"));
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            services.AddSingleton<IServer, Server>();
            services.AddSingleton<WebsocketsHandler>();
            services.AddSingleton<IConfigurationTreeReader, ConfigurationTreeReader>();
            services.AddSingleton<IConfigurationLoader, ConfigurationLoader>();
            services.AddSingleton<IConfigurator, Configurator>();
            services.AddSingleton<IWorkspaceLauncher, WorkspaceLauncher>();
            services.AddSingleton<IWorkspaceInitHook, WorkspaceInitHook>();
            services.AddSingleton<ICloneMaker, CloneMaker>();
            services.AddSingleton<Protocol.Queue<IRingEvent>>();
            services.AddSingleton<ISender<IRingEvent>>(f => f.GetService<Protocol.Queue<IRingEvent>>());
            services.AddSingleton<IReceiver<IRingEvent>>(f => f.GetService<Protocol.Queue<IRingEvent>>());
            services.AddHostedService<ConsoleClient>();
            foreach (var type in GetAllTypesOf<ITool, Startup>()) services.AddTransient(type.AsType());
        }

        public void ConfigureContainer(IServiceContainer container)
        {
            var runnableTypes = Assembly.GetEntryAssembly().GetExportedTypes().Where(t => typeof(IRunnable).IsAssignableFrom(t)).ToList();

            var configMap = (from r in runnableTypes
                             let cfg = r.GetProperty(nameof(Runnable<object, IRunnableConfig>.Config))
                             where cfg != null
                             select (RunnableType: r, ConfigType: cfg.PropertyType)).ToDictionary(x => x.ConfigType, x => x.RunnableType);

            foreach (var rt in configMap.Select(x => x.Value)) { container.Register(rt, rt, new PerRequestLifeTime()); }

            container.Register<IRunnableConfig, IRunnable>((factory, cfg) => (IRunnable)factory.GetInstance(configMap[cfg.GetType()]));

            container.Register(f => TomlSettings.Create(cfg =>
            {
                cfg.ConfigurePropertyMapping(p => p.UseTargetPropertySelector(x => x.IgnoreCase));
            }), new PerContainerLifetime());

            container.Register<Func<LightInject.Scope>>(x => x.BeginScope);
        }

        public void Configure(IApplicationBuilder app)
        {
            ThreadPool.SetMinThreads(100, 100);

            var loggingConfig = new LoggerConfiguration().ReadFrom.Configuration(Configuration);
            var opts = app.ApplicationServices.GetService<BaseOptions>();
            if (opts.IsDebug) loggingConfig.MinimumLevel.Debug();

            Log.Logger = loggingConfig.CreateLogger();
            var logger = app.ApplicationServices.GetService<ILogger<Startup>>();

            using (logger.WithHostScope(Phase.INIT))
            {
                logger.LogInformation("Listening on port {Port}", opts.Port);
            }

            app.UseWebSockets();
            app.UseMiddleware<RingMiddleware>();
        }

        private static IEnumerable<TypeInfo> GetAllTypesOf<T, TAssembly>()
            => from t in typeof(TAssembly).Assembly.DefinedTypes
               where !t.IsAbstract && typeof(T).IsAssignableFrom(t)
               select t;
    }
}
