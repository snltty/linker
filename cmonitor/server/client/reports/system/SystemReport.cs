namespace cmonitor.server.client.reports.system
{
    public sealed class SystemReport : IReport
    {
        public string Name => "System";
        public SystemReport(Config config)
        {
        }

        public object GetReports()
        {
            return null;
        }
    }
   
    public sealed class SystemReportInfo
    {
        public bool Value { get; set; }
    }
}
