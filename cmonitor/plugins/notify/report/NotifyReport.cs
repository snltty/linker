using cmonitor.client.report;

namespace cmonitor.plugins.notify.report
{
    public sealed class NotifyReport : IClientReport
    {
        public string Name => "Notify";

        private readonly INotify notify;
        public NotifyReport(INotify notify)
        {
            this.notify = notify;
        }

        public object GetReports(ReportType reportType)
        {
            return null;
        }

        public void Update(NotifyInfo notify)
        {
            this.notify.Update(notify);
        }
    }
}

