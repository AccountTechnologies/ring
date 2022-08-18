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
    public HashSet<string> DeclaredPaths { get; set; } = new();

    public string path { get; set; }
    public List<AspNetCore> aspnetcore { get; set; } = new();
    public List<IISExpress> iisexpress { get; set; } = new();
    public List<IISXCore> iisxcore { get; set; } = new();
    public List<NetExe> netexe { get; set; } = new();
    public List<DockerCompose> dockercompose { get; set; } = new();
    public List<Kustomize> kustomize { get; set; } = new();
    public List<WorkspaceConfig> import { get; set; } = new();
}
