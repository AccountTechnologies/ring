using System;
using System.Collections.Generic;

namespace ATech.Ring.Configuration.Runnables;

public class Proc : RunnableConfigBase
{
    public string Command { get; set; }
    public string WorkingDir { get; set; }
    public Dictionary<string,string> Env { get; set; } = new();
    public string[] Args { get; set; } = Array.Empty<string>();
    public override string UniqueId => Id ?? Command;
}
