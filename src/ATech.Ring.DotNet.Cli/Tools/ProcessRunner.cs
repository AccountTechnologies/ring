using System;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Tools;

public class ProcessRunner : ITool
{
    public ProcessRunner(ILogger<ITool> logger) => Logger = logger;

    public string Command { get; set; }
    public string[] DefaultArgs { get; set; } = Array.Empty<string>();
    public ILogger<ITool> Logger { get; }
}
