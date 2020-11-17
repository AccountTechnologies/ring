using ATech.Ring.Configuration.Interfaces;
using System;

namespace ATech.Ring.Configuration.Runnables
{
    public class Kustomize : RunnableConfigBase, IUseWorkingDir, IFromGit
    {
        public override string Id => Path;
        public string Path { get; set; }
        public string WorkingDir { get; set; }
        public string FullPath => GetFullPath(WorkingDir, Path);
        
        public override bool Equals(object obj) => obj is Kustomize d && d.Path == Path;
        public override int GetHashCode() => -576574704 + Path.GetHashCode();
        public bool IsRemote() => Path.StartsWith("git@") || Path.StartsWith("ssh://");
        public string SshRepoUrl { get; set;  }
    }
}
