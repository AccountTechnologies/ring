using ATech.Ring.Configuration.Interfaces;
using System.Collections.Generic;

namespace ATech.Ring.Configuration
{
    public class ConfigSet : Dictionary<string, IRunnableConfig>
    {
        public string Path { get; }
        public ConfigSet(string path, Dictionary<string, IRunnableConfig> bareConfigs)
        {
            foreach (var (key, value) in bareConfigs) Add(key, value);
            Path = path;
        }
        public ConfigSet() { }
    }
}