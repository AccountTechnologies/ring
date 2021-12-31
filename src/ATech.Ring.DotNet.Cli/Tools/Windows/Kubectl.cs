using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using ATech.Ring.DotNet.Cli.Tools;
using k8s;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Tools
{
    public class KubectlBundle : ITool
    {
        private readonly Kubernetes _client;
        public KubectlBundle(ILogger<ITool> logger, Kubernetes client)
        {
            _client = client;
            Logger = logger;
        }

        public string ExePath { get; set; } = "wsl";
        public string[] DefaultArgs { get; set; } = Array.Empty<string>();
        public ILogger<ITool> Logger { get; }

        public async Task<bool> IsValidManifestAsync(string filePath)
        {
            var result = await this.RunProcessWaitAsync("kubectl", "apply", "--validate=true", "--dry-run=true", "-f", $"\"{filePath}\"");
            return result.IsSuccess;
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            var result = await this.RunProcessWaitAsync("wslpath", "-w", filePath);
            return result.IsSuccess && File.Exists(result.Output);
        }

        public async Task<ExecutionInfo> KustomizeBuildAsync(string kustomizeDir, string outputFilePath)
        {
            return await this.RunProcessWaitAsync("kustomize", "build", $"\"{kustomizeDir}\"", ">", outputFilePath);
        }

        public async Task<ExecutionInfo> ApplyJsonPathAsync(string path, string jsonPath)
        {
            return await this.RunProcessWaitAsync("kubectl", "apply", "-o", $"jsonpath=\"{jsonPath}\"", "-f", $"\"{path}\"");
        }

        public async Task<string[]> GetPods(string nameSpace) => (await _client.ListNamespacedPodAsync(nameSpace)).Items.Select(x => x.Metadata.Name).ToArray();

        public async Task<string> GetPodStatus(string podName, string nameSpace)
        {
            var pod = await _client.ReadNamespacedPodStatusAsync(podName, nameSpace);
            return pod.Status.Phase;
        }

        public async Task<ExecutionInfo> DeleteAsync(string path)
        {
            return await this.RunProcessWaitAsync("kubectl", "delete", "--ignore-not-found", "-f", $"\"{path}\"");
        }
    }
}