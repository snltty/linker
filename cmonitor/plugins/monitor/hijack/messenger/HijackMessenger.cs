using cmonitor.plugins.hijack.report;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.hijack.messenger
{
    public sealed class HijackClientMessenger : IMessenger
    {
        private readonly HijackReport hijackReport;

        public HijackClientMessenger(HijackReport hijackReport)
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
