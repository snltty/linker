namespace cmonitor.client.reports.volume
{
    public sealed class VolumeReport : IReport
    {
        public string Name => "Volume";
        private VolumeReportInfo report = new VolumeReportInfo();
        private float lastValue;
        private bool lastMute;
        private float lastMasterPeak;

        private readonly Config config;
        private readonly IVolume volume;
        public VolumeReport(Config config, IVolume volume)
        {
            this.config = config;
            this.volume = volume;
        }

        public object GetReports(ReportType reportType)
        {
            report.Value = volume.GetVolume();
            report.Mute = volume.GetMute();
            if (config.VolumeMasterPeak)
            {
                report.MasterPeak = volume.GetMasterPeak();
            }
            if (reportType == ReportType.Full || report.Value != lastValue || report.Mute != lastMute || report.MasterPeak != lastMasterPeak)
            {
                lastValue = report.Value;
                lastMute = report.Mute;
                lastMasterPeak = report.MasterPeak;
                return report;
            }
            return null;
        }


        public void SetVolume(float value)
        {
            volume.SetVolume(value);
        }
        public void SetMute(bool value)
        {
            volume.SetMute(value);
        }
        public void Play(byte[] audioBytes)
        {
            volume.Play(audioBytes);
        }

    }

    public sealed class VolumeReportInfo
    {
        public float Value { get; set; }
        public bool Mute { get; set; }
        public float MasterPeak { get; set; }
    }
}
