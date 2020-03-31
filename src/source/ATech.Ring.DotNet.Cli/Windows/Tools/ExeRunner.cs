using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Tools
{
    public class ExeRunner : ITool
    {
        public ExeRunner(ILogger<ITool> logger) => Logger = logger;

        public string ExePath { get; set; }
        public string[] DefaultArgs { get; set; } = { };
        public ILogger<ITool> Logger { get; }
    }
}