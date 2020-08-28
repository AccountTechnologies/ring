using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using ATech.Ring.DotNet.Cli.Windows.Runnables.Dotnet;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Tools
{
    public class DotnetCliBundle : ITool
    {
        private readonly ExeRunner _exeRunner;
        private readonly GitClone _gitClone;
        public ILogger<ITool> Logger { get; }
        public string ExePath { get; set; } = "dotnet.exe";
        public string[] DefaultArgs { get; set; } = { };
        public Dictionary<string, string> DefaultEnvVars = new Dictionary<string, string> {["ASPNETCORE_ENVIRONMENT"] = "Development"};

        public DotnetCliBundle(ExeRunner exeRunner, GitClone gitClone, ILogger<DotnetCliBundle> logger)
        {
            _exeRunner = exeRunner;
            (_gitClone, Logger) = (gitClone, logger);
        }

        public async Task<ExecutionInfo> RunAsync(DotnetContext ctx, string[] urls = null)
        {          
            HandleUrls();
            if (File.Exists(ctx.ExePath))
            {
                _exeRunner.ExePath = ctx.ExePath;
                return await _exeRunner.RunProcessAsync(ctx.WorkingDir, DefaultEnvVars);
            }
            if (File.Exists(ctx.EntryAssemblyPath))
            {
                // Using dotnet exec here because dotnet run spawns subprocesses and killing it doesn't actually kill them
                return await this.RunProcessAsync(ctx.WorkingDir, DefaultEnvVars, "exec", $"\"{ctx.EntryAssemblyPath}\"");
            }
            throw new InvalidOperationException($"Neither Exe path nor Dll path specified. {ctx.CsProjPath}");

            void HandleUrls()
            {
                if (urls == null) return;
                if (Environment.GetEnvironmentVariable("ASPNETCORE_URLS") == null)
                {
                    DefaultEnvVars.TryAdd("ASPNETCORE_URLS", string.Join(';', urls));
                }
                else
                {
                    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", string.Join(';', urls));
                }
            }
        }

        public async Task<ExecutionInfo> BuildAsync(string csProjFile)
            => await this.RunProcessWaitAsync("build", csProjFile, "-v:q", "/nologo", "/nodereuse:false");

        public Task<ExecutionInfo> GitCloneAsync(IFromGit gitSource) => _gitClone.CloneAsync(gitSource);
    }
}