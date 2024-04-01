using cmonitor.client.report;

namespace cmonitor.plugins.light.report
{
    public sealed class LightReport : IClientReport
    {
        public string Name => "Light";

        LightReportInfo report = new LightReportInfo();

        private readonly ILight light;
        public LightReport(ILight light)
        {
            this.light = light;
        }

        public object GetReports(ReportType reportType)
        {
            report.Value = light.Get();
            if (reportType == ReportType.Full || report.Updated())
            {
                return report;
            }
            return null;
        }

        public void SetLight(int value)
        {
            light.Set(value);
        }
    }

    public sealed class LightReportInfo:ReportInfo
    {
        public int Value { get; set; }

        public override int HashCode()
        {
            return Value.GetHashCode();
        }
    }
}
