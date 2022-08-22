using System;
using Microsoft.Extensions.Configuration;

namespace ATech.Ring.DotNet.Cli.Infrastructure;

using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Infrastructure.Cli;
using ATech.Ring.DotNet.Cli.Workspace;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

public static class WebApplicationExtensions
{
    public static async Task RunRingAsync(this WebApplication app)
    {
        var opts = app.Services.GetRequiredService<BaseOptions>();
        switch (opts)
        {
            case CloneOptions c:
                await app.Services.GetRequiredService<ICloneMaker>().CloneWorkspaceRepos(c.WorkspacePath, c.OutputDir);
                break;
            case ConfigDump:
            {
                var debugView = ((IConfigurationRoot)app.Services.GetRequiredService<IConfiguration>()).GetDebugView();
                Console.WriteLine(debugView);
                break;
            }
            case HeadlessOptions:
            case ConsoleOptions:
                await app.RunAsync(); 
                break;
            default:
                throw new InvalidOperationException("CLI is misconfigured");
        }
    }
}
