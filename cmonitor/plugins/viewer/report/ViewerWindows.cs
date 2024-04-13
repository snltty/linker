using cmonitor.config;
using common.libs;
using Microsoft.Win32;
using System.Net;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Xml;

namespace cmonitor.plugins.viewer.report
{
    [SupportedOSPlatform("windows")]
    public sealed class ViewerWindows : IViewer
    {
        private readonly Config config;
        public ViewerWindows(Config config)
        {
            this.config = config;
        }
        public void Open(bool value, ParamInfo info)
        {
            if (value)
            {
                string str = JsonSerializer.Serialize(info);
                string command = $"start cmonitor.viewer.server.win.exe \"{str.Replace("\"","\\\"")}\"";
                CommandHelper.Windows(string.Empty, new string[] { command }, false);
            }
        }

        public string GetConnectString()
        {
            return Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Cmonitor", "viewerConnectStr", string.Empty).ToString();
        }
        public void SetConnectString(string connectStr)
        {
            Registry.SetValue("HKEY_CURRENT_USER\\SOFTWARE\\Cmonitor", "viewerConnectStr", connectStr);
        }

        public string GetConnectEP(string connectStr)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(connectStr);

                var nodes = xmlDoc.DocumentElement["C"]["T"].ChildNodes;

                var node = nodes[nodes.Count - 3];
                var p = node.Attributes["P"].Value;
                var n = node.Attributes["N"].Value;

                return $"{n}:{p}";
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            return string.Empty;
        }
    }
}
