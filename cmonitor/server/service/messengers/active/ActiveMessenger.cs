using cmonitor.server.api;
using cmonitor.server.client;
using cmonitor.server.client.reports.active;
using cmonitor.server.service.messengers.sign;
using common.libs;
using MemoryPack;

namespace cmonitor.server.service.messengers.active
{
    public sealed class ActiveMessenger : IMessenger
    {
        private readonly ActiveWindowReport activeWindowReport;

        public ActiveMessenger(ActiveWindowReport activeWindowReport)
        {
            this.activeWindowReport = activeWindowReport;
        }

        [MessengerId((ushort)ActiveMessengerIds.Get)]
        public void Get(IConnection connection)
        {
            connection.Write(MemoryPackSerializer.Serialize(activeWindowReport.GetActiveWindowTimes()));
        }

        [MessengerId((ushort)ActiveMessengerIds.Windows)]
        public void Windows(IConnection connection)
        {
            connection.Write(MemoryPackSerializer.Serialize(activeWindowReport.GetWindows()));
        }


        [MessengerId((ushort)ActiveMessengerIds.Clear)]
        public void Clear(IConnection connection)
        {
            activeWindowReport.ClearActiveWindowTimes();
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)ActiveMessengerIds.Disallow)]
        public void Disallow(IConnection connection)
        {
            activeWindowReport.DisallowRun(MemoryPackSerializer.Deserialize<string[]>(connection.ReceiveRequestWrap.Payload.Span));
            connection.Write(Helper.TrueArray);
        }
    }

}
