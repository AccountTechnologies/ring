using System.Collections.Generic;
using ATech.Ring.Configuration.Interfaces;

namespace ATech.Ring.DotNet.Cli.Workspace
{
    public class ConfigurationChangedArgs
    {
        public IDictionary<string, IRunnableConfig> Configuration { get; }
        public ConfigurationChangedArgs(IDictionary<string, IRunnableConfig> configuration) => Configuration = configuration;       
    }
}