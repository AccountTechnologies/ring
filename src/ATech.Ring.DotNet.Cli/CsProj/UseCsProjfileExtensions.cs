using System;
using System.IO;
using System.Text.Json;
using System.Xml;
using System.Xml.XPath;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.DotNet.Cli.CsProj.LaunchSettings;

namespace ATech.Ring.DotNet.Cli.CsProj
{
    public static class UseCsProjFileExtensions
    {
        private const string MsBuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";

        public static string GetWorkingDir(this IUseCsProjFile proj) => new FileInfo(proj.FullPath).DirectoryName;
        public static string GetProjName(this IUseCsProjFile proj) => Path.GetFileNameWithoutExtension(proj.FullPath);
        public static (string framework, string runtime) GetTargetFrameworkAndRuntime(this IUseCsProjFile proj)
        {
            if (proj == null) throw new ArgumentNullException(nameof(proj));
            var xp = new XPathDocument(proj.FullPath);
            var n = xp.CreateNavigator();
            var tf = n.SelectSingleNode("/Project/PropertyGroup/TargetFramework");
            if (tf == null) throw new InvalidOperationException($"TargetFramework is not defined in {proj.FullPath}");

            var ri = n.SelectSingleNode("/Project/PropertyGroup/RuntimeIdentifier");
            return (framework: tf.Value, runtime: ri?.Value);
        }

        private static (XPathNavigator navigator, XmlNamespaceManager nsManager, string nsPrefix) CreateNavigator(this IUseCsProjFile proj)
        {
            if (proj == null) throw new ArgumentNullException(nameof(proj));
            var xp = new XPathDocument(proj.FullPath);
            var nav = xp.CreateNavigator();
            var nsman = new XmlNamespaceManager(new NameTable());
            const string mns = "m:";
            nsman.AddNamespace(mns.Replace(":", ""), MsBuildNs);
            return (nav, nsman, mns);
        }

        public static Uri GetIISUrl(this IUseCsProjFile proj)
        {
            if (File.Exists(proj.LaunchSettingsJsonPath))
            {
                var launchSettings = JsonSerializer.Deserialize<Root>(File.ReadAllText(proj.LaunchSettingsJsonPath));
                var iisExpress = launchSettings.iisSettings.iisExpress;
                var originalUri = iisExpress.applicationUrl;
                var builder = new UriBuilder(originalUri)
                {
                    Port = iisExpress.sslPort != 0 ? iisExpress.sslPort : originalUri.Port, 
                    Scheme = iisExpress.sslPort != 0 ? Uri.UriSchemeHttps : originalUri.Scheme
                };
                return builder.Uri;
            }

            var (nav, nsMan, nsPrefix) = proj.CreateNavigator();

            static string IISUrlXPath(string ns) => $"/{ns}Project/{ns}ProjectExtensions/{ns}VisualStudio/{ns}FlavorProperties/{ns}WebProjectProperties/{ns}IISUrl";

            var iisUrl = nav.SelectSingleNode(IISUrlXPath(nsPrefix), nsMan) ?? nav.SelectSingleNode(IISUrlXPath(string.Empty));
            if (iisUrl != null && !string.IsNullOrWhiteSpace(iisUrl.Value)) return new Uri(iisUrl.Value);

            static string PortXPath(string ns) => $"/{ns}Project/{ns}ProjectExtensions/{ns}VisualStudio/{ns}FlavorProperties/{ns}WebProjectProperties/{ns}DevelopmentServerPort";

            var port = nav.SelectSingleNode(PortXPath(nsPrefix), nsMan) ?? nav.SelectSingleNode(PortXPath(string.Empty));
            if (port == null) throw new InvalidOperationException($"DevelopmentServerPort is not defined in {proj.FullPath}");

            return new Uri($"http://localhost:{port}");
        }
    }
}