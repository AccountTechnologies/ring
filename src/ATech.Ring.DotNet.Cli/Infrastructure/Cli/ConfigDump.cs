namespace ATech.Ring.DotNet.Cli.Infrastructure.Cli;

using CommandLine;

[Verb("config-dump", HelpText = "Configuration dump")]
public class ConfigDump : BaseOptions
{
}

[Verb("config-path", HelpText = "Gets configuration file path")]
public class ConfigPath : BaseOptions
{
    [Option(Group = "scope", MetaValue = "local")]
    public bool Local { get; set; }
    
    [Option(Group = "scope", MetaValue = "user")]
    public bool User { get; set; }
    
    [Option(Group = "scope", MetaValue = "global")]
    public bool Default { get; set; }
}

[Verb("config-create", HelpText = "Creates configuration files")]
public class ConfigCreate : BaseOptions
{
    [Option(Group = "scope", MetaValue = "local")]
    public bool Local { get; set; }
    
    [Option(Group = "scope", MetaValue = "user")]
    public bool User { get; set; }
}
