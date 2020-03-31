using ATech.Ring.Configuration.Interfaces;

namespace ATech.Ring.Configuration.Runnables
{
    public class DockerCompose : RunnableConfigBase, IUseWorkingDir
    {
        public override string Id => Path;
        public string Path { get; set; }
        public string WorkingDir { get; set; }
        public string FullPath => GetFullPath(WorkingDir, Path);
        public override bool Equals(object obj)
        {
            return obj is DockerCompose d && d.Path == Path;
        }

        public override int GetHashCode()
        {
            return -576574704 + Path.GetHashCode();
        }
    }
}