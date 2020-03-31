using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Tools
{
    public class MsBuild : ITool
    {
        public MsBuild(ILogger<MsBuild> logger) => Logger = logger;

        public string ExePath { get; set; } = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\msbuild.exe";
        public string[] DefaultArgs { get; set; } = {"/nologo", "/v:q"};
        public ILogger<ITool> Logger { get; }
        public async Task<ExecutionInfo> BuildAsync(string csProjFile) => await this.RunProcessWaitAsync(csProjFile, "/nodereuse:false", "/p:Configuration=Debug");
        public async Task<ExecutionInfo> RestoreAsync(string csProjFile) => await this.RunProcessWaitAsync(csProjFile, "/nodereuse:false", "/p:Configuration=Debug", "/t:Restore");
    }
}