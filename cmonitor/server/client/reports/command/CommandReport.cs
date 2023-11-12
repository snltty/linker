namespace cmonitor.server.client.reports.command
{
    public sealed class CommandReport : IReport
    {
        public string Name => "Command";

        private readonly CommandReportInfo commandReportInfo = new CommandReportInfo();
        public CommandReport()
        {
        }

        public object GetReports(ReportType reportType)
        {
            return null;
        }
    }

    public sealed class CommandReportInfo
    {
        //public string[] RegeditUsers { get; set; } = Array.Empty<string>();
    }


}
