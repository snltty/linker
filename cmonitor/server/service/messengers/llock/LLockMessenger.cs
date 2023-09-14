using cmonitor.server.client.reports.llock;
using cmonitor.server.service.messengers.sign;
using MemoryPack;

namespace cmonitor.server.service.messengers.llock
{
    public sealed class LLockMessenger : IMessenger
    {
        private readonly LLockReport lLockReport;

        public LLockMessenger(LLockReport lLockReport)
        {
            this.lLockReport = lLockReport;
        }

        [MessengerId((ushort)LLockMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            lLockReport.Update(MemoryPackSerializer.Deserialize<bool>(connection.ReceiveRequestWrap.Payload.Span));
        }
    }

}
