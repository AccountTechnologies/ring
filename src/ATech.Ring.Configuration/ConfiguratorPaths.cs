using System.Runtime.InteropServices;

namespace ATech.Ring.Configuration
{
    public class ConfiguratorPaths
    {
        private const string WslMnt = "/mnt/";
        private string _path;
        public string WorkspacePath
        {
            get => _path;
            set
            {
                if (value.StartsWith(WslMnt) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var parts = value.Replace(WslMnt, "").Split("/");
                    _path = parts[0] + ":\\" + string.Join("\\", parts[1..]);
                }
                else
                {
                    _path = value;
                }
            }
        }
    }
}