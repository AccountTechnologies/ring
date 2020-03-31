using System;
using System.IO;
using System.Runtime.Serialization;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions.Context;
using ATech.Ring.DotNet.Cli.CsProj;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.Dotnet
{
    public class DotnetContext : ICsProjContext, ITrackRetries, ITrackProcessId, ITrackProcessOutput
    {
        public int ProcessId { get; set; }
        public string Output { get; set; }
        public string CsProjPath { get; set; }
        public string WorkingDir { get; set; }
        public string TargetFramework { get; set; }
        public string TargetRuntime { get; set; }
        public string EntryAssemblyPath { get; set; }
        public string ExePath => Path.ChangeExtension(EntryAssemblyPath, "exe");
        public int ConsecutiveFailures { get; set; }
        public int TotalFailures { get; set; }
        public static T Create<T,C>(C config) where C : IUseCsProjFile where  T : DotnetContext
        {
            var ctx = (T)FormatterServices.GetUninitializedObject(typeof(T));
            
            if (config is IFromGit {SshRepoUrl: string _, CloneFullPath: string c})
            {
                if (Path.IsPathRooted(config.CsProj))
                {
                    throw new InvalidOperationException($"If sshRepoUrl is used csProj must be a relative path but it is {config.CsProj}");
                }
                config.CsProj = Path.Combine(c, config.CsProj);
            }
            ctx.CsProjPath = config.CsProj;
            (ctx.TargetFramework, ctx.TargetRuntime) = config.GetTargetFrameworkAndRuntime();
            ctx.WorkingDir = config.GetWorkingDir();
            var runtimePathSegment = ctx.TargetRuntime == null ? "" : $"\\{ctx.TargetRuntime}";
            ctx.EntryAssemblyPath = Path.Combine(ctx.WorkingDir, $"bin\\Debug\\{ctx.TargetFramework}{runtimePathSegment}\\{config.GetProjName()}.dll");
            return ctx;
        }
    }

}