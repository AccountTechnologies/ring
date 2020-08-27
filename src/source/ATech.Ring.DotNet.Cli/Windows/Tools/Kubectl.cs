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

        public async Task<ExecutionInfo> ApplyKJsonPathAsync(string kustomizeDir, string jsonPath)
        {
            return await this.RunProcessWaitAsync("kustomize", "build", $"\"{kustomizeDir}\"", "|", "kubectl", "apply", "-o", $"jsonpath=\"{jsonPath}\"", "-f", "-");
        }

        public async Task<ExecutionInfo> GetResources(string kind, string nameSpace)
        {
            return await this.RunProcessWaitAsync("kubectl", "get", kind, "-o", "name", "-n", nameSpace);
        }

        public async Task<ExecutionInfo> GetPodStatus(string podName, string nameSpace)
        {
            return await this.RunProcessWaitAsync("kubectl", "get", podName, "-o", "jsonpath='{.status.phase}'", "-n", nameSpace);
        }

        public async Task<ExecutionInfo> DeleteKAsync(string kustomizeDir)
        {
            return await this.RunProcessWaitAsync("kustomize", "build", $"\"{kustomizeDir}\"", "|", "kubectl", "delete", "-o", "name", "-f", "-");
        }
    }
}
