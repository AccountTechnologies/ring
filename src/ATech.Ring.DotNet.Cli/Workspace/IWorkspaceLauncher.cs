using System;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.Configuration;
using ATech.Ring.DotNet.Cli.Dtos;
using ATech.Ring.Protocol.v2.Events;

namespace ATech.Ring.DotNet.Cli.Workspace
{
    public interface IWorkspaceLauncher
    {
        Task LoadAsync(ConfiguratorPaths paths, CancellationToken token);
        Task StartAsync(CancellationToken token);
        Task StopAsync(CancellationToken token);
        Task WaitUntilStoppedAsync(CancellationToken token);
        Task UnloadAsync(CancellationToken token);
        Task<ExcludeResult> ExcludeAsync(string id, CancellationToken token);
        Task<IncludeResult> IncludeAsync(string id, CancellationToken token);
        string WorkspacePath { get; }
        void PublishStatus(ServerState serverState);
        event EventHandler OnInitiated;
    }
}