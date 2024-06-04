using cmonitor.plugins.viewer.config;
using common.libs;
using System.Net;

namespace cmonitor.plugins.viewer.report
{
    public interface IViewer
    {
        public void Open(bool value, ParamInfo info);
        public string GetConnectString();
        public void SetConnectString(string connectStr);
        public IPEndPoint GetConnectEP(string connectStr)
        {
            return null;
        }
    }

    public sealed class ParamInfo
    {
        public string ShareMkey { get; set; } = "cmonitor/share";
        public int ShareMLength { get; set; } = 10;
        public int ShareItemMLength { get; set; } = 1024;
        public int ShareIndex { get; set; } = 5;
        public ViewerMode Mode { get; set; } = ViewerMode.Server;
        public string GroupName { get; set; } = Helper.GlobalString;
    }
}


