using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.Configuration.Runnables;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ATech.Ring.Configuration;

public class WorkspaceConfig : IWorkspaceConfig
{
    public static PropertyInfo[] Properties<T>() => typeof(WorkspaceConfig).GetProperties().Where(pi => typeof(IEnumerable<T>).IsAssignableFrom(pi.PropertyType)).ToArray();

    public T[] Elements<T>() => Properties<T>().SelectMany(x => (IEnumerable<T>)x.GetValue(this) ?? new T[] { }).ToArray();
    public WorkspaceConfig Parent { get; set; }

    public string Id => string.IsNullOrWhiteSpace(path) ? "" : System.IO.Path.GetFullPath(path);
    public HashSet<string> DeclaredPaths { get; set; } = new HashSet<string>();

    public string path { get; set; }
    public AspNetCore[] aspnetcore { get; set; } = { };
    public IISExpress[] iisexpress { get; set; } = { };
    public IISXCore[] iisxcore { get; set; } = { };
    public NetExe[] netexe { get; set; } = { };
    public DockerCompose[] dockercompose { get; set; } = { };
    public Kustomize[] kustomize { get; set; } = { };
    public string[] imports { get; set; } = { };
    public List<WorkspaceConfig> import { get; set; } = new();
}
