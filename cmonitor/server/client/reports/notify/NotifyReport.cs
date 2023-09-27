using common.libs;
using MemoryPack;

namespace cmonitor.server.client.reports.notify
{
    public sealed class NotifyReport : IReport
    {
        public string Name => "Notify";

        public NotifyReport()
        {
        }

        public object GetReports()
        {
            return null;
        }

        public void Update(NotifyInfo notify)
        {
            Task.Run(() =>
            {
                CommandHelper.Windows(string.Empty, new string[] {
                        $"start notify.win.exe {notify.Speed} \"{notify.Msg}\""
                    });
            });
        }
    }

    [MemoryPackable]
    public sealed partial class NotifyInfo
    {
        public byte Speed { get; set; }
        public string Msg { get; set; }
    }
}

