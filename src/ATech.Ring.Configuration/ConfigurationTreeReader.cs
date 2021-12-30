using System.IO;
using ATech.Ring.Configuration.Interfaces;

namespace ATech.Ring.Configuration;

public class ConfigurationTreeReader : IConfigurationTreeReader
{
    private readonly IConfigurationLoader _loader;

    public ConfigurationTreeReader(IConfigurationLoader loader)
    {
        _loader = loader;
    }

    public WorkspaceConfig GetConfigTree(ConfiguratorPaths paths)
    {
        var file = new FileInfo(Path.GetFullPath(paths.WorkspacePath));

        var rootDir = file.DirectoryName;

        return Populate(file.Name, null, rootDir);

        WorkspaceConfig Populate(string path, WorkspaceConfig parent, string currentDirectory)
        {
            var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(currentDirectory, path);
            var c = _loader.Load<WorkspaceConfig>(fullPath);
            if (c == null) return new WorkspaceConfig();

            c.Parent = parent;
            c.path = fullPath;

            foreach (var r in c.Elements<IRunnableConfig>())
            {
                if (r is IUseWorkingDir wd) wd.WorkingDir = new FileInfo(c.path).DirectoryName;
                r.DeclaredPaths.Add(fullPath);
            }
            if (c.import == null) return c;
            for (var i = 0; i < c.import.Length; i++)
            {
                ref var w = ref c.import[i];
                w = Populate(w.path, c, new FileInfo(fullPath).DirectoryName);
            }

            return c;
        }
    }
}
