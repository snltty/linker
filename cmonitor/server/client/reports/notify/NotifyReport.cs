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

        public object GetReports(ReportType reportType)
        {
            return null;
        }

        public void Update(NotifyInfo notify)
        {
            Task.Run(() =>
            {
                CommandHelper.Windows(string.Empty, new string[] {
                        $"start notify.win.exe {notify.Speed} \"{notify.Msg}\" {notify.Star}"
                    });
            });
        }
    }

    [MemoryPackable]
    public sealed partial class NotifyInfo
    {
        public byte Speed { get; set; }
        public string Msg { get; set; }
        public byte Star { get; set; } = 1;
    }
}

