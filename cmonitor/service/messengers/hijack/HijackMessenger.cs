using cmonitor.client.reports.hijack;
using MemoryPack;

namespace cmonitor.service.messengers.hijack
{
    public sealed class HijackMessenger : IMessenger
    {
        private readonly HijackReport hijackReport;

        public HijackMessenger(HijackReport hijackReport)
        {
            this.hijackReport = hijackReport;
        }

        [MessengerId((ushort)HijackMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            HijackSetRuleInfo info = MemoryPackSerializer.Deserialize<HijackSetRuleInfo>(connection.ReceiveRequestWrap.Payload.Span);
            hijackReport.Update(info);
        }
    }
}
