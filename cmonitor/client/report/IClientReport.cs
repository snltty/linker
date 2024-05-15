namespace cmonitor.client.report
{
    public interface IClientReport
    {
        public string Name { get; }

        public object GetReports(ReportType reportType);
    }
}
