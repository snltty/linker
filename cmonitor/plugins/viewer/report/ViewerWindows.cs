using cmonitor.config;
using common.libs;
using Microsoft.Win32;
using System.Runtime.Versioning;

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
        public void Open(bool value, ViewerMode mode)
        {
            if (value)
            {
                string command = $"start cmonitor.viewer.server.win.exe {config.Client.ShareMemoryKey} {config.Client.ShareMemoryCount} {config.Client.ShareMemorySize} {(int)ShareMemoryIndexs.Viewer} {(byte)mode}";
                CommandHelper.Windows(string.Empty, new string[] { command }, false);
            }
            else
            {
                //CommandHelper.Windows(string.Empty, new string[] { $"taskkill /f /im cmonitor.viewer.server.win.exe" });
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
    }
}
