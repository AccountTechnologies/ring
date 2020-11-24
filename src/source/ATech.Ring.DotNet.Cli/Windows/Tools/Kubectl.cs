using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Tools
{
    public class KubectlBundle : ITool
    {
        public KubectlBundle(ILogger<ITool> logger) => Logger = logger;

        public string ExePath { get; set; } = "wsl";
        public string[] DefaultArgs { get; set; } = { };
        public ILogger<ITool> Logger { get; }

        public async Task<bool> IsValidManifestAsync(string filePath)
        {
            var result = await this.RunProcessWaitAsync("kubectl", "apply", "--validate=true", "--dry-run=true", "-f", $"\"{filePath}\"");
            return result.IsSuccess;
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            var result = await this.RunProcessWaitAsync($"[ -f \"{filePath}\" ] && echo \"true\" || echo \"false\"");
            return bool.Parse(result.Output);
        }

        public async Task<ExecutionInfo> KustomizeBuildAsync(string kustomizeDir, string outputFilePath)
        {
            return await this.RunProcessWaitAsync("kustomize", "build", $"\"{kustomizeDir}\"", ">", outputFilePath);
        }

        public async Task<ExecutionInfo> ApplyJsonPathAsync(string path, string jsonPath)
        {
            return await this.RunProcessWaitAsync("kubectl", "apply", "-o", $"jsonpath=\"{jsonPath}\"", "-f", $"\"{path}\"");
        }

        public async Task<ExecutionInfo> GetResources(string kind, string nameSpace)
        {
            return await this.RunProcessWaitAsync("kubectl", "get", kind, "-o", "name", "-n", nameSpace);
        }

        public async Task<ExecutionInfo> GetPodStatus(string podName, string nameSpace)
        {
            return await this.RunProcessWaitAsync("kubectl", "get", podName, "-o", "jsonpath='{.status.phase}'", "-n", nameSpace);
        }

        public async Task<ExecutionInfo> DeleteAsync(string path)
        {
            return await this.RunProcessWaitAsync("kubectl", "delete", "--ignore-not-found", "-f", $"\"{path}\"");
        }
    }
}
