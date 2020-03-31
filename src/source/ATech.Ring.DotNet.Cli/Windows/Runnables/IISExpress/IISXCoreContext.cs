using System;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.Abstractions.Context;
using ATech.Ring.DotNet.Cli.CsProj;
using ATech.Ring.DotNet.Cli.Windows.Runnables.Dotnet;

namespace ATech.Ring.DotNet.Cli.Windows.Runnables.IISExpress
{
    public class IISXCoreContext : DotnetContext, ITrackUri
    {
        public string TempAppHostConfigPath { get; set; }
        public Uri Uri { get; set; }
        public static IISXCoreContext Create<C>(C config) where C : IUseCsProjFile
        {
            var ctx = Create<IISXCoreContext, C>(config);
            ctx.Uri = config.GetIISUrl();
            return ctx;
        }
    }
}