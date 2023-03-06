using ATech.Ring.Configuration.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace ATech.Ring.Configuration;

public static class WorkspaceConfigExtensions
{
    public static ConfigSet ToEffectiveConfig(this WorkspaceConfig root)
    {
        return new ConfigSet(root.path, GetEffectiveConfig(root).ToDictionary(x => x.UniqueId, x => x));

        IEnumerable<IRunnableConfig> GetEffectiveConfig(WorkspaceConfig node)
        {
            if (node == null) return new IRunnableConfig[] { };
            var configs = node.Elements<IRunnableConfig>()
                .GroupBy(x => x.UniqueId)
                .Select(x => x.First())
                .ToArray();

            if (node.import == null) return configs;

            var nested = (from w in node.import select GetEffectiveConfig(w)).SelectMany(w => w).ToArray();
            return configs.Concat(nested.Except(configs));
        }
    }
}
