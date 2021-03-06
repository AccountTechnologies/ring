using ATech.Ring.Configuration.Interfaces;
using System.Collections.Generic;
using System.IO;

namespace ATech.Ring.Configuration.Runnables
{
    public abstract class RunnableConfigBase : IRunnableConfig
    {
        public abstract string Id { get; }
        public HashSet<string> DeclaredPaths { get; set; } = new HashSet<string>();
        public static string GetFullPath(string workDir, string path) {

          path = path.Replace("file://", "");
          return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(workDir, path));
        }
    }
}