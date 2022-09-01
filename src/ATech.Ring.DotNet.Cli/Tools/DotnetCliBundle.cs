using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using ATech.Ring.DotNet.Cli.Runnables.Dotnet;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Tools;

public class DotnetCliBundle : ITool
{
    private const string UrlsEnvVar = "ASPNETCORE_URLS";
    private readonly ProcessRunner _processRunner;
    public ILogger<ITool> Logger { get; }
    public string Command { get; set; } = "dotnet";
    public string[] DefaultArgs { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> DefaultEnvVars = new() { ["ASPNETCORE_ENVIRONMENT"] = "Development"};
        
    public DotnetCliBundle(ProcessRunner processRunner, ILogger<DotnetCliBundle> logger)
    {
        _processRunner = processRunner;
        Logger = logger;
    }

    public async Task<ExecutionInfo> RunAsync(DotnetContext ctx, CancellationToken token, string[]? urls = null)
    {          
        HandleUrls();
        if (File.Exists(ctx.ExePath))
        {
            _processRunner.Command = ctx.ExePath;
            return await _processRunner.RunProcessAsync(ctx.WorkingDir, DefaultEnvVars, null, token);
        }
        if (File.Exists(ctx.EntryAssemblyPath))
        {
            // Using dotnet exec here because dotnet run spawns subprocesses and killing it doesn't actually kill them
            return await this.RunProcessAsync(ctx.WorkingDir, DefaultEnvVars, new object[] { "exec", $"\"{ctx.EntryAssemblyPath}\"" }, token);
        }
        throw new InvalidOperationException($"Neither Exe path nor Dll path specified. {ctx.CsProjPath}");

        void HandleUrls()
        {
            if (urls == null) return;
            if (Environment.GetEnvironmentVariable(UrlsEnvVar) == null)
            {
                DefaultEnvVars.TryAdd(UrlsEnvVar, string.Join(';', urls));
            }
            else
            {
                Environment.SetEnvironmentVariable(UrlsEnvVar, string.Join(';', urls));
            }
        }
    }

    public async Task<ExecutionInfo> BuildAsync(string csProjFile, CancellationToken token)
        => await this.RunProcessWaitAsync(new object[] { "build", csProjFile, "-v:q", "/nologo", "/nodereuse:false" }, token);
}
