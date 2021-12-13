using System;
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
            var getPath = await this.RunProcessWaitAsync("wslpath", "-w", wslPath).ContinueWith(async x =>
            {
                var output = await x;
                return output.Output;
            });

            var delay = Task.Delay(3000);
            var t = await Task.WhenAny(getPath, delay);
            // This may happen when WSL fails to start and blows up after 30 seconds with
            // the following error: The Windows Subsystem for Linux instance has terminated.
            if (t == delay) throw new InvalidOperationException("Could not execute WSL command within the acceptable timeout");
            return await getPath;
        }
    }
}
