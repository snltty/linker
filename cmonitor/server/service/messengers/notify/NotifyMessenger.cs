using cmonitor.server.client.reports.notify;
using MemoryPack;

namespace cmonitor.server.service.messengers.notify
{
    public sealed class NotifyMessenger : IMessenger
    {
        private readonly NotifyReport notifyReport;

        public NotifyMessenger(NotifyReport notifyReport)
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
