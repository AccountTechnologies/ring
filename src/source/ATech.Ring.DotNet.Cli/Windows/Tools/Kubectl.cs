using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Tools
{
    public class Kubectl : ITool
    {
        public Kubectl(ILogger<ITool> logger) => Logger = logger;

        public string ExePath { get; set; } = "kubectl";
        public string[] DefaultArgs { get; set; } = { };
        public ILogger<ITool> Logger { get; }

        public async Task<ExecutionInfo> ApplyKAsync(string kustomizeDir)
        {
            return await this.RunProcessWaitAsync("apply", "-k", $"\"{kustomizeDir}\"");
        }

        public async Task<ExecutionInfo> DeleteKAsync(string kustomizeDir)
        {
            return await this.RunProcessWaitAsync("delete", "-k", $"\"{kustomizeDir}\"");
        }
    }
}
