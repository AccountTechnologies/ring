using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Infrastructure.Cli;
using ATech.Ring.DotNet.Cli.Workspace;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    public static class WebApplicationExtensions
    {        
        public static async Task RunRingAsync(this WebApplication app)
        {
            var opts = app.Services.GetRequiredService<BaseOptions>();
            if (opts is CloneOptions c)
            {
                await app.Services.GetRequiredService<ICloneMaker>().CloneWorkspaceRepos(c.WorkspacePath, c.OutputDir);
            }
            else
            {
                await app.RunAsync();
            }
        }
    }
}
