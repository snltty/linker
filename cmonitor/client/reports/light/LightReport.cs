namespace cmonitor.client.reports.light
{
    public sealed class LightReport : IReport
    {
        public string Name => "Light";

        LightReportInfo report = new LightReportInfo();
        int lastValue = 0;

        private readonly ILight light;
        public LightReport(ILight light)
        {
            this.light = light;
        }

        public object GetReports(ReportType reportType)
        {
            report.Value = light.Get();
            if (reportType == ReportType.Full || report.Value != lastValue)
            {
                lastValue = report.Value;
                return report;
            }
            return null;
        }

        public void SetLight(int value)
        {
            light.Set(value);
        }
    }

    public sealed class LightReportInfo
    {
        public int Value { get; set; }
    }
}
