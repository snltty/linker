using cmonitor.client.reports.command;
using cmonitor.client.reports.llock;
using cmonitor.service.messengers.command;
using cmonitor.service.messengers.sign;
using MemoryPack;

namespace cmonitor.service.messengers.llock
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
            //lLockReport.LockSystem();
        }

    }

}
