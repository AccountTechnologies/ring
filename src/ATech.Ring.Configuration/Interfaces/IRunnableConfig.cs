using System.Collections.Generic;

namespace ATech.Ring.Configuration.Interfaces;

public interface IRunnableConfig : IWorkspaceConfig
{
    string FriendlyName { get; }
    List<string> Tags { get; }
}
