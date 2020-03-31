using ATech.Ring.Configuration.Interfaces;
using System.Collections.Generic;

namespace ATech.Ring.Configuration
{
    public class ConfigSet : Dictionary<string, IRunnableConfig>
    {
        public string Path { get; }
        public ConfigSet(string path, Dictionary<string, IRunnableConfig> bareConfigs)
        {
            foreach (var c in bareConfigs) Add(c.Key, c.Value);
            Path = path;
        }
        public ConfigSet() { }
    }
}