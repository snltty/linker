namespace cmonitor.server.client.reports.light
{
    public sealed class LightReport : IReport
    {
        public string Name => "Light";

        private readonly LightWatcher lightWatcher;
        public LightReport()
        {

            if (OperatingSystem.IsWindows())
            {
                lightWatcher = new LightWatcher();
                lightWatcher.BrightnessChanged += (e, value) =>
                {
                    dic["Value"] = value.newBrightness;
                };
                dic["Value"] = LightWmiHelper.GetBrightnessLevel();
            }
        }

        Dictionary<string, object> dic = new Dictionary<string, object> { { "Value", 0 } };
        public Dictionary<string, object> GetReports()
        {
            return dic;
        }

        public void SetLight(int value)
        {
            LightWmiHelper.SetBrightnessLevel(value);
        }
    }
}
