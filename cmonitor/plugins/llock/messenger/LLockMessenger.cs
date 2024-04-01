using cmonitor.plugins.llock.report;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.llock.messenger
{
    public sealed class LLockClientMessenger : IMessenger
    {
        private readonly LLockReport lLockReport;

        public LLockClientMessenger(LLockReport lLockReport)
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
