using cmonitor.client.args;
using cmonitor.plugins.viewer.report;
using common.libs.winapis;

namespace cmonitor.plugins.viewer.proxy
{
    public sealed class ViewerProxySignInArgs : ISignInArgs
    {
        public void Invoke(Dictionary<string, string> args)
        {
            string userName = GetUserName();
            args[ViewerConfigInfo.userNameKey] = userName;
        }

        private string GetUserName()
        {
            if (OperatingSystem.IsWindows())
            {
                return Win32Interop.GetUserName();
            }
            return string.Empty;
        }
    }
}
