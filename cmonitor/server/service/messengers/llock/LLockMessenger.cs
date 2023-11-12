using cmonitor.server.client.reports.command;
using cmonitor.server.client.reports.llock;
using cmonitor.server.service.messengers.command;
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

        [MessengerId((ushort)LLockMessengerIds.LockScreen)]
        public void LockScreen(IConnection connection)
        {
            lLockReport.LockScreen(MemoryPackSerializer.Deserialize<bool>(connection.ReceiveRequestWrap.Payload.Span));
        }

        [MessengerId((ushort)LLockMessengerIds.LockSystem)]
        public void LockSystem(IConnection connection)
        {
            lLockReport.LockSystem();
        }

    }

}
