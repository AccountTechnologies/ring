using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Abstractions.Tools;
using ATech.Ring.DotNet.Cli.Tools;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Windows.Tools
{
    public class IISExpressExe : ITool
    {
        public string ExePath { get; set; } = "C:\\Program Files\\IIS Express\\iisexpress.exe";
        public string[] DefaultArgs { get; set; } = Array.Empty<string>();
        public ILogger<ITool> Logger { get; }
        public IISExpressExe(ILogger<IISExpressExe> logger) => Logger = logger;

        private void OnError(string error)
        {
            if (error == null) return;
            var message = error.StartsWith("Failed to register URL") ? $"The port might be used another process. Try 'netstat -na -o' or if ports are reserved 'netsh interface ipv4 show excludedportrange protocol=tcp'). Original error: {error}" : $"IISExpress failed. Original error: {error}";

            Logger.LogError(message);
        }

        public async Task<ExecutionInfo> StartWebsite(string configPath, IDictionary<string,string>? envVars = null)
        {
            return await this.RunProcessAsync(OnError, envVars, $"/config:\"{configPath}\"", $"/siteid:1");
        }
    }
}
