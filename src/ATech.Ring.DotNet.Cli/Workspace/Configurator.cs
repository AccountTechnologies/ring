using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Logging;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Workspace
{
    public class Configurator : IConfigurator, IDisposable
    {
        private readonly IConfigurationTreeReader _configReader;
        private readonly ILogger<Configurator> _logger;
        private static readonly object FswLock = new object();
        public ConfigSet Current { get; private set; } = new ConfigSet();
        private FileSystemWatcher _currentWatcher;

        public Configurator(IConfigurationTreeReader configReader, ILogger<Configurator> logger)
        {
            _configReader = configReader;
            _logger = logger;
        }

        public event EventHandler<ConfigurationChangedArgs> OnConfigurationChanged;

        public bool TryGet(string key, out IRunnableConfig cfg) => Current.TryGetValue(key, out cfg);

        public async Task LoadAsync(ConfiguratorPaths paths, CancellationToken token)
        {
            Current = await LoadConfigurationAsync(paths);
            _currentWatcher = Watch(paths.WorkspacePath, async () =>
            {
                OnConfigurationChanged?.Invoke(this, new ConfigurationChangedArgs(await LoadConfigurationAsync(paths)));
            });
        }

        public Task UnloadAsync(CancellationToken token)
        {
            _currentWatcher?.Dispose();
            Current = new ConfigSet();
            return Task.CompletedTask;
        }

        private static FileSystemWatcher Watch(string path, Func<Task> react)
        {
            var directoryName = Path.GetDirectoryName(path);

            if (directoryName == null) throw new InvalidOperationException($"Workspace path does not have a directory ({path}).");

            var fsw = new FileSystemWatcher
            {
                Path = directoryName,
                Filter = Path.GetFileName(path),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };

            fsw.Changed += async (sender, args) =>
            {
                var w = (FileSystemWatcher)sender;
                if (!w.EnableRaisingEvents) return;
                lock (FswLock)
                {
                    if (!w.EnableRaisingEvents) return;
                    w.EnableRaisingEvents = false;
                }

                try
                {
                    await react();
                }
                finally
                {
                    w.EnableRaisingEvents = true;
                }
            };

            fsw.EnableRaisingEvents = true;
            return fsw;
        }

        private async Task<ConfigSet> LoadConfigurationAsync(ConfiguratorPaths paths)
        {
            WorkspaceConfig tree = null;
            const int retries = 5;
            var tryCount = retries;
            while (tryCount > 0)
            {
                try
                {
                    tryCount--;
                    tree = _configReader.GetConfigTree(paths);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not open workspace '{paths.WorkspacePath}'. {tryCount} retries left");
                    await Task.Delay(1000);
                }
            }

            if (tree == null) throw new FileLoadException($"Could not (re)load workspace after {retries} tries. Path: '{paths.WorkspacePath}'");
            var effectiveConfig = tree.ToEffectiveConfig();

            using (_logger.WithHostScope(Phase.CONFIG))
            {
                _logger.LogInformation("Workspace: {WorkspaceFile}", paths.WorkspacePath);
                _logger.LogInformation(PhaseStatus.OK);
            }

            Current = effectiveConfig;
            return effectiveConfig;
        }

        public void Dispose()
        {
            _currentWatcher?.Dispose();
        }
    }
}
