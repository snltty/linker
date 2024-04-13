using System.Net;

namespace cmonitor.plugins.viewer.report
{
    public sealed class ViewerMacOS : IViewer
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

        public string GetConnectEP(string connectStr)
        {
            return string.Empty;
        }
    }
}
