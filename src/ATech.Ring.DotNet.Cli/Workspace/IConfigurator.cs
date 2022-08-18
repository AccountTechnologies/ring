using System;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration;
using ATech.Ring.Configuration.Interfaces;

namespace ATech.Ring.DotNet.Cli.Workspace;

public interface IConfigurator
{
    event EventHandler<ConfigurationChangedArgs> OnConfigurationChanged;
    Task LoadAsync(ConfiguratorPaths paths, CancellationToken token);
    Task UnloadAsync(CancellationToken token);
    bool TryGet(string key, out IRunnableConfig cfg);
    ConfigSet Current { get; }
}