using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using ATech.Ring.Configuration;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using ATech.Ring.DotNet.Cli.Infrastructure;
using ATech.Ring.DotNet.Cli.Infrastructure.Cli;
using ATech.Ring.DotNet.Cli.Logging;
using ATech.Ring.DotNet.Cli.Workspace;
using ATech.Ring.Protocol.v2;
using k8s;
using LightInject;
using LightInject.Microsoft.AspNetCore.Hosting;
using LightInject.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nett;
using Serilog;
using Serilog.Events;
using Tomlyn.Extensions.Configuration;

static string Ring(string ver) => $"       _ _\n     *'   '*\n    * .*'*. 3\n   '  @   a  ;     ring! v{ver}\n    * '*.*' *\n     *. _ .*\n";
var originalWorkingDir = Directory.GetCurrentDirectory();

try
{
    ThreadPool.SetMinThreads(100, 100);
    Log.Logger = new LoggerConfiguration().WriteTo.Console()
                                            .MinimumLevel.Information()
                                            .MinimumLevel.Override("Microsoft", LogEventLevel.Error).CreateLogger();

    var options = CliParser.GetOptions(args, originalWorkingDir);

    if (!options.NoLogo)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        Console.WriteLine(Ring(version.ToString()));
    }

    var containerOptions = new ContainerOptions
    {
        EnablePropertyInjection = false,
        EnableVariance = false
    };

    IServiceContainer container = new ServiceContainer(containerOptions)
    {
        ScopeManagerProvider = new PerLogicalCallContextScopeManagerProvider()
    };

    var clients = new ConcurrentDictionary<Uri, HttpClient>();

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSingleton(options);
    if (options is ServeOptions) builder.Services.AddSingleton(f => (ServeOptions)f.GetRequiredService<BaseOptions>());
    builder.Services.AddSingleton<Func<Uri, HttpClient>>(_ => uri => clients.GetOrAdd(uri, new HttpClient { BaseAddress = uri, MaxResponseContentBufferSize = 1 }));
    builder.Services.AddOptions();
    builder.Services.Configure<RingConfiguration>(builder.Configuration);
    builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
    builder.Services.AddSingleton<IServer, Server>();
    builder.Services.AddSingleton<WebsocketsHandler>();
    builder.Services.AddSingleton<IConfigurationTreeReader, ConfigurationTreeReader>();
    builder.Services.AddSingleton<IConfigurationLoader, ConfigurationLoader>();
    builder.Services.AddSingleton<IConfigurator, Configurator>();
    builder.Services.AddSingleton<IWorkspaceLauncher, WorkspaceLauncher>();
    builder.Services.AddSingleton<IWorkspaceInitHook, WorkspaceInitHook>();
    builder.Services.AddSingleton<ICloneMaker, CloneMaker>();
    builder.Services.AddSingleton<ATech.Ring.Protocol.v2.Queue>();
    builder.Services.AddSingleton<ISender>(f => f.GetRequiredService<ATech.Ring.Protocol.v2.Queue>());
    builder.Services.AddSingleton<IReceiver>(f => f.GetRequiredService<ATech.Ring.Protocol.v2.Queue>());
    builder.Services.AddSingleton(f =>
    {
        var configuredPath = f.GetRequiredService<IOptions<RingConfiguration>>().Value.Kubernetes.ConfigPath;
        var maybeKubeconfigEnv = Environment.GetEnvironmentVariable("KUBECONFIG");
        var configPath = maybeKubeconfigEnv ?? configuredPath ?? throw new InvalidOperationException("Kubernetes config path is not set"); 
        return new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile(configPath));
    });

    builder.Services.AddHostedService<WebsocketsInitializer>();
    builder.Services.AddSingleton<ConsoleClient>();

    static IEnumerable<TypeInfo> GetAllTypesOf<T, TAssembly>()
     => from t in typeof(TAssembly).Assembly.DefinedTypes
        where !t.IsAbstract && typeof(T).IsAssignableFrom(t)
        select t;

    foreach (var type in GetAllTypesOf<ITool, Program>()) builder.Services.AddTransient(type.AsType());

    builder.Host.UseSerilog();

    builder.Host.ConfigureContainer<IServiceContainer>((ctx, container) =>
    {
        var runnableTypes = Assembly.GetEntryAssembly().GetExportedTypes().Where(t => typeof(IRunnable).IsAssignableFrom(t)).ToList();

        var configMap = (from r in runnableTypes
                         let cfg = r.GetProperty(nameof(Runnable<object, IRunnableConfig>.Config))
                         where cfg != null
                         select (RunnableType: r, ConfigType: cfg.PropertyType)).ToDictionary(x => x.ConfigType, x => x.RunnableType);

        foreach (var (_, rt) in configMap) { container.Register(rt, rt, new PerRequestLifeTime()); }

        container.Register<IRunnableConfig, IRunnable>((factory, cfg) =>
        {
            var ct = configMap[cfg.GetType()];
            var ctor = ct.GetConstructors().Single();
            var args = ctor.GetParameters().Select(x =>
               typeof(IRunnableConfig).IsAssignableFrom(x.ParameterType) ? cfg : factory.GetInstance(x.ParameterType)).ToArray();

            return (IRunnable)ctor.Invoke(args);
        });

        container.Register(_ => TomlSettings.Create(cfg =>
        {
            cfg.ConfigurePropertyMapping(p => p.UseTargetPropertySelector(x => x.IgnoreCase));
        }), new PerContainerLifetime());

        container.Register<Func<LightInject.Scope>>(x => x.BeginScope);
    });

    builder.Host.ConfigureAppConfiguration((_, b) =>
    {
        b.Sources.Clear();
        b.AddTomlFile(Directories.Installation.SettingsPath, optional: false);
        b.AddTomlFile(Directories.Installation.LoggingPath, optional: false);
        b.AddTomlFile(Directories.User.SettingsPath, optional: true);
        b.AddTomlFile(Directories.Working(originalWorkingDir).SettingsPath, optional: true);
        b.AddEnvironmentVariables("RING_");
        if (options is ServeOptions { Port: var port }) b.AddInMemoryCollection(new Dictionary<string, string> { ["ring:port"] = port.ToString() });
    });
    builder.Host.UseServiceProviderFactory(new LightInjectServiceProviderFactory());
    builder.WebHost.UseLightInject(container);
    builder.WebHost.SuppressStatusMessages(true);

    var app = builder.Build();
    app.Urls.Add($"http://0.0.0.0:{app.Configuration.GetValue<int>("ring:port")}");

    var loggingConfig = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration);

    if (options.IsDebug) loggingConfig.MinimumLevel.Debug();

    Log.Logger = loggingConfig.CreateLogger();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    using (logger.WithHostScope(Phase.INIT))
    {
        if (options is ServeOptions { Port: var port }) logger.LogInformation("Listening on port {Port}", port.ToString());
    }

    app.UseWebSockets();
    app.UseMiddleware<RingMiddleware>();

    app.Lifetime.ApplicationStarted.Register(async () =>
        await app.Services.GetRequiredService<ConsoleClient>().StartAsync(app.Lifetime.ApplicationStopping)
    );

    app.Lifetime.ApplicationStopping.Register(async () =>
        await app.Services.GetRequiredService<ConsoleClient>().StopAsync(app.Lifetime.ApplicationStopped)
    );

    await app.RunRingAsync();
}
catch (FileNotFoundException x) when (x.FileName == CliParser.DefaultFileName)
{
    Console.WriteLine($"ERROR: {x.Message}");
    Environment.ExitCode = 1;
}
catch (Exception ex)
{
    Log.Logger.Fatal($"Unhandled exception: {ex}");
    Environment.ExitCode = -1;
}
finally
{
    Directory.SetCurrentDirectory(originalWorkingDir);
}
