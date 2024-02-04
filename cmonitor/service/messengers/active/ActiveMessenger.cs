using cmonitor.api;
using cmonitor.client;
using cmonitor.client.reports.active;
using cmonitor.service.messengers.sign;
using common.libs;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.service.messengers.active
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
            activeWindowReport.DisallowRun(MemoryPackSerializer.Deserialize<ActiveDisallowInfo>(connection.ReceiveRequestWrap.Payload.Span));
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)ActiveMessengerIds.Kill)]
        public void Kill(IConnection connection)
        {
            activeWindowReport.Kill(connection.ReceiveRequestWrap.Payload.Span.ToInt32());
        }
    }

}
