using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Tools
{
    public class DockerCompose : ITool
    {
        public DockerCompose(ILogger<ITool> logger) => Logger = logger;

        public string ExePath { get; set; } = "docker-compose";
        public string[] DefaultArgs { get; set; } = { };
        public ILogger<ITool> Logger { get; }

        public async Task<ExecutionInfo> RmAsync(string composeFilePath)
        {
            return await this.RunProcessWaitAsync("-f", $"\"{composeFilePath}\"", "rm", "-f");
        }

        public async Task<ExecutionInfo> PullAsync(string composeFilePath)
        {
            return await this.RunProcessWaitAsync("-f", $"\"{composeFilePath}\"", "pull");
        }

        public async Task<ExecutionInfo> UpAsync(string composeFilePath)
        {
            return await this.RunProcessAsync("-f", $"\"{composeFilePath}\"", "up", "--force-recreate");
        }

        public async Task<ExecutionInfo> DownAsync(string composeFilePath)
        {
            return await this.RunProcessWaitAsync("-f", $"\"{composeFilePath}\"", "down");
        }

        public async Task<ExecutionInfo> StopAsync(string composeFilePath)
        {
            return await this.RunProcessWaitAsync("-f", $"\"{composeFilePath}\"", "stop");
        }
    }
}