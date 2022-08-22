namespace ATech.Ring.DotNet.Cli.Infrastructure.Cli;

using CommandLine;

public class ServeOptions : BaseOptions
{
    [Option('p', "port", Default = 7999,Required = false, HelpText = "Specify sever port (defaults to 7999)")]
    public int Port { get; set; }
}

public class BaseOptions
{
    [Option('n', "no-logo", Required = false, HelpText = "Hides the startup logo")]
    public bool NoLogo { get; set; }
    [Option('d', "debug", Required = false, HelpText = "Runs debug mode")]
    public bool IsDebug { get; set; }
}
