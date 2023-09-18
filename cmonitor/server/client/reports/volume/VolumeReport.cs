using common.libs;
#if DEBUG || RELEASE
using NAudio.CoreAudioApi;
#endif

namespace cmonitor.server.client.reports.volume
{
    public sealed class VolumeReport : IReport
    {
        public string Name => "Volume";
        private VolumeReportInfo report = new VolumeReportInfo();
        public VolumeReport()
        {
            Volume();
        }

        public object GetReports()
        {
            report.Value = GetVolume();
            report.Mute = GetVolumeMute();
            report.MasterPeak = GetMasterPeakValue();
            return report;
        }

#if DEBUG || RELEASE
        MMDeviceEnumerator enumerator;
        MMDevice device;
        AudioEndpointVolume volumeObject;
#endif
        private void Volume()
        {

            if (OperatingSystem.IsWindows())
            {
                try
                {
#if DEBUG || RELEASE
                    enumerator = new MMDeviceEnumerator();
                    device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                    volumeObject = device.AudioEndpointVolume;
#endif
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }
        }
        private float GetVolume()
        {
            try
            {
#if DEBUG || RELEASE
                if (volumeObject != null)
                {
                    return volumeObject.MasterVolumeLevelScalar * 100;
                }
#endif
            }
            catch (Exception)
            {
            }
            return -1;
        }
        private float GetMasterPeakValue()
        {
            try
            {
#if DEBUG || RELEASE
                if (device != null)
                {
                    return device.AudioMeterInformation.MasterPeakValue * 100;
                }
#endif
            }
            catch (Exception)
            {
            }
            return -1;

        }
        private bool GetVolumeMute()
        {
            try
            {
#if DEBUG || RELEASE
                if (volumeObject != null)
                {
                    return volumeObject.Mute;
                }
#endif
            }
            catch (Exception)
            {
            }
            return false;
        }
        public void SetVolume(float volume)
        {
            try
            {
#if DEBUG || RELEASE
                volume = Math.Max(0, Math.Min(1, volume));
                if (volumeObject != null)
                {
                    volumeObject.MasterVolumeLevelScalar = volume;
                }
#endif
            }
            catch (Exception)
            {
            }
        }
        public void SetVolumeMute(bool mute)
        {
            try
            {
#if DEBUG || RELEASE
                if (volumeObject != null)
                {
                    volumeObject.Mute = mute;
                }
#endif
            }
            catch (Exception)
            {
            }
        }
    }

    public sealed class VolumeReportInfo
    {
        public float Value { get; set; }
        public bool Mute { get; set; }
        public float MasterPeak { get; set; }
    }
}
