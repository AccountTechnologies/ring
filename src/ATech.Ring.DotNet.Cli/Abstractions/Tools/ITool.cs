using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Abstractions.Tools;

public interface ITool
{
    string Command { get; set; }
    string[] DefaultArgs { get; set; }
    ILogger<ITool> Logger { get; }
}
