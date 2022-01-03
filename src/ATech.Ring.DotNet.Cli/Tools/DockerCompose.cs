using System;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Tools
{
    public class DockerCompose : ITool
    {
        public DockerCompose(ILogger<ITool> logger) => Logger = logger;

        public string ExePath { get; set; } = "docker-compose";
        public string[] DefaultArgs { get; set; } = Array.Empty<string>();
        public ILogger<ITool> Logger { get; }

        public async Task<ExecutionInfo> RmAsync(string composeFilePath, CancellationToken token)
        {
            return await this.RunProcessWaitAsync(token, "-f", $"\"{composeFilePath}\"", "rm", "-f");
        }

        public async Task<ExecutionInfo> PullAsync(string composeFilePath, CancellationToken token)
        {
            return await this.RunProcessWaitAsync(token, "-f", $"\"{composeFilePath}\"", "pull");
        }

        public async Task<ExecutionInfo> UpAsync(string composeFilePath, CancellationToken token)
        {
            return await this.RunProcessAsync(token, "-f", $"\"{composeFilePath}\"", "up", "--force-recreate");
        }

        public async Task<ExecutionInfo> DownAsync(string composeFilePath, CancellationToken token)
        {
            return await this.RunProcessWaitAsync(token, "-f", $"\"{composeFilePath}\"", "down");
        }

        public async Task<ExecutionInfo> StopAsync(string composeFilePath, CancellationToken token)
        {
            return await this.RunProcessWaitAsync(token, "-f", $"\"{composeFilePath}\"", "stop");
        }
    }
}