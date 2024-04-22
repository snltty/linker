using cmonitor.plugins.wlan.report;
using cmonitor.server;

namespace cmonitor.plugins.wlan.messenger
{
    public sealed class WlanClientMessenger : IMessenger
    {
        public WlanClientMessenger()
        {
        }


        [MessengerId((ushort)WlanMessengerIds.Get)]
        public void Get(IConnection connection)
        {
        }

        [MessengerId((ushort)WlanMessengerIds.Set)]
        public void Set(IConnection connection)
        {
        }
    }

}
