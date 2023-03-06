using ATech.Ring.Configuration.Interfaces;
using System.Collections.Generic;
using System.IO;

namespace ATech.Ring.Configuration.Runnables;

public abstract class RunnableConfigBase : IRunnableConfig
{
    public abstract string UniqueId { get; }
    public string? FriendlyName { get; set; }
    /// <summary>
    /// If implemented in derived class enables overriding the default <see cref="UniqueId"/>
    /// </summary>
    public string? Id { get; set; }
    public HashSet<string> DeclaredPaths { get; set; } = new();

    public List<string> Tags { get; set; } = new();

    public static string GetFullPath(string workDir, string path)
    {

        path = path.Replace("file://", "");
        return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(workDir, path));
    }
}
