using cmonitor.plugins.notify.report;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.notify.messenger
{
    public sealed class NotifyClientMessenger : IMessenger
    {
        private readonly NotifyReport notifyReport;

        public NotifyClientMessenger(NotifyReport notifyReport)
        {
            this.notifyReport = notifyReport;
        }

        [MessengerId((ushort)NotifyMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            notifyReport.Update(MemoryPackSerializer.Deserialize<NotifyInfo>(connection.ReceiveRequestWrap.Payload.Span));
        }
    }

}
