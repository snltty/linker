namespace cmonitor.client.reports.light
{
    public sealed class LightWindows : ILight
    {
        private readonly LightWindowsWatcher lightWatcher;
        private int value = 0;
        public LightWindows(Config config)
        {
            if (config.IsCLient)
            {
                lightWatcher = new LightWindowsWatcher();
                lightWatcher.BrightnessChanged += (e, value) =>
                {
                    this.value = (int)value.newBrightness;
                };
                value = LightWindowsWmiHelper.GetBrightnessLevel();
            }
        }

        public int Get()
        {
            return value;
        }

        public void Set(int value)
        {
            LightWindowsWmiHelper.SetBrightnessLevel(value);
        }
    }
}
