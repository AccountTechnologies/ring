using ATech.Ring.Configuration.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace ATech.Ring.Configuration;

public class ConfigSet : Dictionary<string, IRunnableConfig>
{
    public const string AllFlavours = "__all";
    public HashSet<string> Flavours { get; } = new();
    public string Path { get; }
    public ConfigSet(string path, Dictionary<string, IRunnableConfig> bareConfigs)
    {
        foreach (var (key, value) in bareConfigs)
        {
            value.Tags.Add(AllFlavours);
            Add(key, value);
        }
        Path = path;
        Flavours = new HashSet<string>(bareConfigs.Values.SelectMany(x => x.Tags));
    }
    public ConfigSet() { }
}
