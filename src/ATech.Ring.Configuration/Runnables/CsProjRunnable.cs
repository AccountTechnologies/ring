using System.IO;
using ATech.Ring.Configuration.Interfaces;

namespace ATech.Ring.Configuration.Runnables;

public abstract class CsProjRunnable : RunnableConfigBase, IUseCsProjFile, IFromGit
{
    public string WorkingDir { get; set; }
    public string CsProj { get; set; }
    public string SshRepoUrl { get; set; }
    public string FullPath => GetFullPath(WorkingDir, CsProj);
    public string LaunchSettingsJsonPath => Path.Combine(Path.GetDirectoryName(FullPath), "Properties/launchSettings.json");
    private string _id;
    public override string UniqueId => _id ??= Id ?? Path.GetFileNameWithoutExtension(CsProj);
}
