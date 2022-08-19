namespace ATech.Ring.DotNet.Cli.Infrastructure;

using System;

public class RingConfiguration
{
    public string? GitCloneRootPath { get; set; }
    public string? KustomizeCacheRootPath { get; set; }
    public HooksConfiguration? Hooks { get; set; }
    public int SpreadFactorMilliseconds { get; set; } = 1500;
}

public class InitHookConfig
{
    public string? Command { get; set; }
    public string[] Args { get; set; } = Array.Empty<string>();
}

public class HooksConfiguration
{
    public InitHookConfig? Init { get; set; }
}
