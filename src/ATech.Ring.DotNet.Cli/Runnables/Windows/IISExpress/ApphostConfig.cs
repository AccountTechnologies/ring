using System;
using System.IO;
using System.Xml;

namespace ATech.Ring.DotNet.Cli.Runnables.Windows.IISExpress;

public class ApphostConfig
{
    public string VirtualDir { get; set; }
    public Uri Uri { get; set; }
    private const string ApphostConfigTemplatePath = "C:/Program Files/IIS Express/AppServer/applicationhost.config";
    private static string IISExpressTempDir => Path.Combine(Path.GetTempPath(), "iisexpress");
    private static readonly Lazy<XmlDocument> ApphostConfigTemplate = new Lazy<XmlDocument>(() =>
    {
        var xml = new XmlDocument();
        xml.Load(ApphostConfigTemplatePath);
        Optimize(xml);
        return xml;
    });

    public string Ensure()
    {
        Directory.CreateDirectory(IISExpressTempDir);

        var apphostConfig = (XmlDocument)ApphostConfigTemplate.Value.Clone();

        var siteNode = apphostConfig.SelectSingleNode("/configuration/system.applicationHost/sites/site[@id='1']");

        siteNode.SelectSingleNode("application/virtualDirectory").Attributes["physicalPath"].Value = VirtualDir;
        var binding = siteNode.SelectSingleNode("bindings/binding");
        binding.Attributes["bindingInformation"].Value = $":{Uri.Port}:{Uri.Host}";
        binding.Attributes["protocol"].Value = Uri.Scheme;
        var cfgPath = Path.Combine(IISExpressTempDir, $"applicationhost{Guid.NewGuid():n}.config");
           
        apphostConfig.Save(cfgPath);
        return cfgPath;
    }

    private static void Optimize(XmlDocument config)
    {
        CommentOut("/configuration/system.webServer/globalModules/add[@name='StaticCompressionModule']");
        CommentOut("/configuration/system.webServer/globalModules/add[@name='HttpLoggingModule']");
        CommentOut("/configuration/system.webServer/globalModules/add[@name='FailedRequestsTracingModule']");
        CommentOut("/configuration/location/system.webServer/modules/add[@name='HttpLoggingModule']");
        CommentOut("/configuration/location/system.webServer/modules/add[@name='FailedRequestsTracingModule']");
        CommentOut("/configuration/location/system.webServer/modules/add[@name='StaticCompressionModule']");
        SetValue("/configuration/system.applicationHost/sites/siteDefaults/traceFailedRequestsLogging/@enabled", "false");

        void SetValue(string xpath, string value)
        {
            var node = config.SelectSingleNode(xpath);
            if (string.Compare(node.Value, value, StringComparison.OrdinalIgnoreCase) != 0) return;
            node.Value = value;
        }

        void CommentOut(string xpath)
        {
            var nodeToRemove = config.SelectSingleNode(xpath);
            if (nodeToRemove == null) return;
            var parent = nodeToRemove.ParentNode;
            parent.ReplaceChild(config.CreateComment(nodeToRemove.OuterXml), nodeToRemove);
        }

    }
}