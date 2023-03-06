using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Tools;

public class KustomizeTool : ITool
{
    public KustomizeTool(ILogger<ITool> logger)
    {
        Logger = logger;
    }
    public string Command { get; set; } = "kustomize";
    public string[] DefaultArgs { get; set; } = Array.Empty<string>();
    public ILogger<ITool> Logger { get; }
    
    public async Task<ExecutionInfo> BuildAsync(string kustomizeDir, string outputFilePath, CancellationToken token)
    {
        var output = await this.RunProcessWaitAsync(new object[] { "build", kustomizeDir }, token);
        await File.WriteAllTextAsync(outputFilePath, output.Output, token);
        return output;
    }
}
