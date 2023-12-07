namespace cmonitor.client.reports.command
{
    public sealed class CommandReport : IReport
    {
        public string Name => "Command";

        public CommandReport()
        {
        }

        public object GetReports(ReportType reportType)
        {
            return null;
        }
    }

}
