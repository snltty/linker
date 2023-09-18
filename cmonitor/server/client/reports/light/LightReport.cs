namespace cmonitor.server.client.reports.light
{
    public sealed class LightReport : IReport
    {
        public string Name => "Light";
        LightReportInfo report = new LightReportInfo();

        private readonly LightWatcher lightWatcher;
        public LightReport()
        {

            if (OperatingSystem.IsWindows())
            {
                lightWatcher = new LightWatcher();
                lightWatcher.BrightnessChanged += (e, value) =>
                {
                    report.Value = (int)value.newBrightness;
                };
                report.Value = LightWmiHelper.GetBrightnessLevel();
            }
        }

        public object GetReports()
        {
            return report;
        }

        public void SetLight(int value)
        {
            LightWmiHelper.SetBrightnessLevel(value);
        }
    }

    public sealed class LightReportInfo
    {
        public int Value { get; set; }
    }
}
