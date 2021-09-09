using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Tools
{
    public class Wsl : ITool
    {
        public Wsl(ILogger<ITool> logger)
        {
            Logger = logger;
        }

        public string ExePath { get; set; } = "wsl";
        public string[] DefaultArgs { get; set; }
        public ILogger<ITool> Logger { get; }

        public async Task<string> ResolveToWindows(string wslPath)
        {
            var result = await this.RunProcessWaitAsync("wslpath", "-w", wslPath);
            return result.Output;
        }
    }
}
