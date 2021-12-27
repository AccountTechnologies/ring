using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Infrastructure;
using ATech.Ring.DotNet.Cli.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ATech.Ring.DotNet.Cli.Workspace
{
    public class WorkspaceInitHook : IWorkspaceInitHook
    {
        private readonly ILogger<WorkspaceInitHook> _logger;
        private readonly ExeRunner _runner;
        private readonly bool _configured;
        public WorkspaceInitHook(ILogger<WorkspaceInitHook> logger, ExeRunner runner, IOptions<RingConfiguration> opts)
        {
            _logger = logger;
            _runner = runner;
            var config = opts?.Value?.Hooks?.Init;
            if (!(config is { Command: string c, Args: string[] args })) return;
            _configured = true;
            _runner.ExePath = c;
            _runner.DefaultArgs = args;
        }

        public async Task RunAsync(CancellationToken token)
        {
            if (_configured)
            {
                _logger.LogDebug("Executing Workspace Init Hook");
                await _runner.RunProcessWaitAsync();
            }
            else
            {
                _logger.LogDebug("Workspace Init Hook not configurred. Skipping.");
            }
        }
    }
}