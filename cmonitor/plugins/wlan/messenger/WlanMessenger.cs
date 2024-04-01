using cmonitor.plugins.wlan.report;
using cmonitor.server;

namespace cmonitor.plugins.wlan.messenger
{
    public sealed class WlanClientMessenger : IMessenger
    {
        private readonly WlanReport wlanReport;
        public WlanClientMessenger(WlanReport wlanReport)
        {
            this.wlanReport = wlanReport;
        }


        [MessengerId((ushort)WlanMessengerIds.Get)]
        public void Get(IConnection connection)
        {
            //var wifis = wlanReport.WlanEnums();
            //connection.Write(MemoryPackSerializer.Serialize(wifis));
        }

        [MessengerId((ushort)WlanMessengerIds.Set)]
        public void Set(IConnection connection)
        {
            //WlanSetInfo value = MemoryPackSerializer.Deserialize<WlanSetInfo>(connection.ReceiveRequestWrap.Payload.Span);
            //wlanReport.WlanConnect(value);
        }
    }

}
