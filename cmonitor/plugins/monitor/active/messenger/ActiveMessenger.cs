using cmonitor.plugins.active.report;
using cmonitor.server;
using common.libs;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.plugins.active.messenger
{
    public sealed class ActiveClientMessenger : IMessenger
    {
        private readonly ActiveWindowReport activeWindowReport;

        public ActiveClientMessenger(ActiveWindowReport activeWindowReport)
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
            activeWindowReport.Kill(connection.ReceiveRequestWrap.Payload.Span.ToUInt32());
        }
    }

}
