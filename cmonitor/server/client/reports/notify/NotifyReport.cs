namespace cmonitor.server.client.reports.notify
{
    public sealed class NotifyReport : IReport
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

