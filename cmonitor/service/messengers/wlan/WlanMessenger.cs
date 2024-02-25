using cmonitor.client.reports.wlan;
using MemoryPack;

namespace cmonitor.service.messengers.wlan
{
    public sealed class WlanMessenger : IMessenger
    {
        private readonly WlanReport wlanReport;
        public WlanMessenger(WlanReport wlanReport)
        {
            this.wlanReport = wlanReport;
        }


        [MessengerId((ushort)WlanMessengerIds.Get)]
        public void Get(IConnection connection)
        {
            var wifis = wlanReport.WlanEnums();
            connection.Write(MemoryPackSerializer.Serialize(wifis));
        }

        [MessengerId((ushort)WlanMessengerIds.Set)]
        public void Set(IConnection connection)
        {
            WlanSetInfo value = MemoryPackSerializer.Deserialize<WlanSetInfo>(connection.ReceiveRequestWrap.Payload.Span);
            wlanReport.WlanConnect(value);
        }
    }

}
