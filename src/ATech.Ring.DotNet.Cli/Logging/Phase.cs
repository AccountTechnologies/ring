// ReSharper disable InconsistentNaming
namespace ATech.Ring.DotNet.Cli.Logging;

public enum Phase
{
    INIT = 0,
    START = 1,
    HEALTH = 2,
    RECOVERY = 3,
    STOP = 4,
    DESTROY = 5,
    EXCLUDE = 6,
    CONFIG = 7,
    GIT = 8
}