using cmonitor.client.report;
using cmonitor.config;
using cmonitor.plugins.volume.report;

namespace cmonitor.plugins.volume.report
{
    public sealed class VolumeReport : IClientReport
    {
        public string Name => "Volume";
        private VolumeReportInfo report = new VolumeReportInfo();

        private readonly IVolume volume;
        private Config config;

        public VolumeReport(Config config, IVolume volume)
        {
            this.config = config;
            this.volume = volume;
        }

        public object GetReports(ReportType reportType)
        {
            report.Value = volume.GetVolume();
            report.Mute = volume.GetMute();
            if (config.Data.Client.Volume.MasterPeak)
            {
                report.MasterPeak = volume.GetMasterPeak();
            }
            if (reportType == ReportType.Full || report.Updated())
            {
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

    public sealed class VolumeReportInfo : ReportInfo
    {
        public float Value { get; set; }
        public bool Mute { get; set; }
        public float MasterPeak { get; set; }

        public override int HashCode()
        {
            return Value.GetHashCode() ^ Mute.GetHashCode() ^ MasterPeak.GetHashCode();
        }
    }

    public sealed class VolumeConfigInfo
    {
        public bool MasterPeak { get; set; }
    }

}

namespace cmonitor.config
{
    public sealed partial class ConfigClientInfo
    {
        public VolumeConfigInfo Volume { get; set; } = new VolumeConfigInfo();
    }
}