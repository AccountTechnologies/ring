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
            return await this.RunProcessWaitAsync(new object[] { "-f", $"\"{composeFilePath}\"", "rm", "-f" }, token);
        }

        public async Task<ExecutionInfo> PullAsync(string composeFilePath, CancellationToken token)
        {
            return await this.RunProcessWaitAsync(new object[] { "-f", $"\"{composeFilePath}\"", "pull" }, token);
        }

        public async Task<ExecutionInfo> UpAsync(string composeFilePath, CancellationToken token)
        {
            return await this.RunProcessAsync(new object[] { "-f", $"\"{composeFilePath}\"", "up", "--force-recreate" }, token);
        }

        public async Task<ExecutionInfo> DownAsync(string composeFilePath, CancellationToken token)
        {
            return await this.RunProcessWaitAsync(new object[] { "-f", $"\"{composeFilePath}\"", "down" }, token);
        }

        public async Task<ExecutionInfo> StopAsync(string composeFilePath, CancellationToken token)
        {
            return await this.RunProcessWaitAsync(new object[] { "-f", $"\"{composeFilePath}\"", "stop" }, token);
        }
    }
}