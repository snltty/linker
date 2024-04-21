using System.Net;

namespace cmonitor.plugins.viewer.report
{
    public sealed class ViewerLinux : IViewer
    {
        public void Open(bool value, ParamInfo info)
        {
        }
        public string GetConnectString()
        {
            return string.Empty;
        }
        public void SetConnectString(string connectStr)
        {

        }

        public IPEndPoint GetConnectEP(string connectStr)
        {
            return null;
        }
    }
}
