using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using ATech.Ring.DotNet.Cli.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Events;

namespace ATech.Ring.DotNet.Cli
{
    public class Program
    {
        private static string Ring(string ver) => $"       _ _\n     *'   '*\n    * .*'*. 3\n   '  @   a  ;     ring! v{ver}\n    * '*.*' *\n     *. _ .*\n";
        public static async Task Main(string[] args)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine(Ring(version.ToString()));

            try
            {
                Log.Logger = new LoggerConfiguration().WriteTo.Console()
                                                      .MinimumLevel.Information()
                                                      .MinimumLevel.Override("Microsoft", LogEventLevel.Error).CreateLogger();
                var hostBuilder = CreateWebHostBuilder(args).Build();
                await hostBuilder.RunRingAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal($"Unhandled exception: {ex}");
            }
            finally
            {
                Directory.SetCurrentDirectory(Startup.OriginalWorkingDir);
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) => new WebHostBuilder().ForRing(args);
    }
}