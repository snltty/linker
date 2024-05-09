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
        public ViewerWindows()
        {
        }
        public void Open(bool value, ParamInfo info)
        {
            if (value)
            {
                string str = JsonSerializer.Serialize(info);
                string command = $"start ./plugins/viewer/cmonitor.viewer.server.win.exe \"{str.Replace("\"", "\\\"")}\"";
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

        public IPEndPoint GetConnectEP(string connectStr)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(connectStr);

                var nodes = xmlDoc.DocumentElement["C"]["T"].ChildNodes;
                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];
                    var p = node.Attributes["P"].Value;
                    var n = node.Attributes["N"].Value;

                    IPAddress ip = IPAddress.Parse(n);
                    if(ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return new IPEndPoint(ip, int.Parse(p));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            return null;
        }
    }
}
