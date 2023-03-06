using ATech.Ring.Configuration.Interfaces;

namespace ATech.Ring.Configuration.Runnables;

public class DockerCompose : RunnableConfigBase, IUseWorkingDir, IFromGit
{
    public override string UniqueId => Id ?? Path;
    public string Path { get; set; }
    public string WorkingDir { get; set; }
    public string FullPath => GetFullPath(WorkingDir, Path);
    public override bool Equals(object obj) => obj is DockerCompose d && d.Path == Path;
    public override int GetHashCode() => -576574704 + Path.GetHashCode();
    public string SshRepoUrl { get; set; }
}
