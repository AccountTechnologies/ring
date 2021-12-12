using System.Collections.Generic;

namespace ATech.Ring.Configuration.Interfaces
{
    public interface IWorkspaceConfig 
    {
        string Id { get; }
        HashSet<string> DeclaredPaths { get; set; } 
    }
}