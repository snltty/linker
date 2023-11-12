using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace cmonitor.server.client.reports.volume
{
    public sealed class VolumeReport : IReport
    {
        public string Name => "Volume";
        private VolumeReportInfo report = new VolumeReportInfo();
        private float lastValue;
        private bool lastMute;
        private float lastMasterPeak;

        private readonly Config config;
        public VolumeReport(Config config)
        {
            this.config = config;
            Init();
        }

        public object GetReports(ReportType reportType)
        {
            if (OperatingSystem.IsWindows())
            {
                report.Value = GetVolume();
                report.Mute = GetVolumeMute();
                if (config.VolumeMasterPeak)
                {
                    report.MasterPeak = GetMasterPeakValue();
                }
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



        private float GetVolume()
        {
            try
            {
                return defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100;
            }
            catch (Exception)
            {
            }
            return 0;
        }
        public void SetVolume(float volume)
        {
            try
            {

                volume = Math.Max(0, Math.Min(volume, 1));
                defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
            }
            catch (Exception)
            {
            }
        }

        public bool GetVolumeMute()
        {
            try
            {
                return defaultDevice.AudioEndpointVolume.Mute;
            }
            catch (Exception)
            {
            }
            return false;
        }
        public bool SetVolumeMute(bool mute)
        {
            try
            {
                defaultDevice.AudioEndpointVolume.Mute = mute;
            }
            catch (Exception)
            {
            }
            return false;
        }

        private float GetMasterPeakValue()
        {
            try
            {
                return information.MasterPeakValue * 100;
            }
            catch (Exception)
            {
            }
            return 0;
        }

        public void PlayAudio(byte[] audioBytes)
        {
            using WaveFileReader stream = new WaveFileReader(new MemoryStream(audioBytes));
            using WaveOutEvent player = new WaveOutEvent();
            player.Init(stream);
            player.Play();

            while (player.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(100);
            }
        }

        MMDeviceEnumerator deviceEnumerator;
        MMDevice defaultDevice;
        AudioMeterInformation information;
        private void Init()
        {
            if (OperatingSystem.IsWindows())
            {
                deviceEnumerator = new MMDeviceEnumerator();
                defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                information = defaultDevice.AudioMeterInformation;
            }
        }

        /*
        private IntPtr pEnumerator = IntPtr.Zero;
        private IntPtr pDevice = IntPtr.Zero;
        private IntPtr pEndpointVolume = IntPtr.Zero;
        private IntPtr pMeterInfo = IntPtr.Zero;
        private float GetVolume()
        {
            try
            {
                if (pEndpointVolume != IntPtr.Zero)
                {
                    return GetSystemVolume(pEndpointVolume) * 100;
                }
            }
            catch (Exception)
            {
            }
            return 0;
        }
        private float GetMasterPeakValue()
        {
            try
            {
                if (pMeterInfo != IntPtr.Zero)
                {
                    return GetSystemMasterPeak(pMeterInfo) * 100;
                }
            }
            catch (Exception)
            {
            }
            return 0;

        }
        private bool GetVolumeMute()
        {
            try
            {
                if (pEndpointVolume != IntPtr.Zero)
                {
                    return GetSystemMute(pEndpointVolume);
                }
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
                volume = Math.Max(0, Math.Min(1, volume));
                if (pEndpointVolume != IntPtr.Zero)
                {
                    SetSystemVolume(pEndpointVolume, volume);
                }
            }
            catch (Exception)
            {
            }
        }
        public void SetVolumeMute(bool mute)
        {
            try
            {
                if (pEndpointVolume != IntPtr.Zero)
                {
                    SetSystemMute(pEndpointVolume, mute);
                }
            }
            catch (Exception)
            {
            }
        }

        private void Init()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    pEnumerator = InitSystemDeviceEnumerator();
                    if (pEnumerator != IntPtr.Zero)
                    {
                        pDevice = InitSystemDevice(pEnumerator);
                        if (pDevice != IntPtr.Zero)
                        {
                            pEndpointVolume = InitSystemAudioEndpointVolume(pDevice);
                            pMeterInfo = InitSystemAudioMeterInformation(pDevice);
                        }
                    }
                    FreeDevice();
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }
        }
        private void FreeVolume()
        {
            FreeSystemDevice(pEnumerator, pDevice, pEndpointVolume, pMeterInfo);
            pEnumerator = IntPtr.Zero;
            pDevice = IntPtr.Zero;
            pEndpointVolume = IntPtr.Zero;
            pMeterInfo = IntPtr.Zero;
        }
        private void FreeDevice()
        {
            FreeSystemDevice(pEnumerator, pDevice, IntPtr.Zero, IntPtr.Zero);
            pEnumerator = IntPtr.Zero;
            pDevice = IntPtr.Zero;
        }

        ~VolumeReport()
        {
            FreeVolume();
            pEnumerator = IntPtr.Zero;
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (disposing && pEnumerator != IntPtr.Zero)
            {
                FreeVolume();
            }
        }

        [DllImport("cmonitor.volume.dll")]
        public static extern IntPtr InitSystemDeviceEnumerator();
        [DllImport("cmonitor.volume.dll")]
        public static extern IntPtr InitSystemDevice(IntPtr pEnumerator);
        [DllImport("cmonitor.volume.dll")]
        public static extern IntPtr InitSystemAudioEndpointVolume(IntPtr pDevice);
        [DllImport("cmonitor.volume.dll")]
        public static extern IntPtr InitSystemAudioMeterInformation(IntPtr pDevice);
        [DllImport("cmonitor.volume.dll")]
        public static extern bool FreeSystemDevice(IntPtr pEnumerator, IntPtr pDevice, IntPtr pEndpointVolume, IntPtr pMeterInfo);

        [DllImport("cmonitor.volume.dll")]
        public static extern float GetSystemVolume(IntPtr pEndpointVolume);
        [DllImport("cmonitor.volume.dll")]
        public static extern bool SetSystemVolume(IntPtr pEndpointVolume, float volume);
        [DllImport("cmonitor.volume.dll")]
        public static extern float GetSystemMasterPeak(IntPtr pMeterInfo);
        [DllImport("cmonitor.volume.dll")]
        public static extern bool GetSystemMute(IntPtr pEndpointVolume);
        [DllImport("cmonitor.volume.dll")]
        public static extern bool SetSystemMute(IntPtr pEndpointVolume, bool mute);
        */
    }

    public sealed class VolumeReportInfo
    {
        public float Value { get; set; }
        public bool Mute { get; set; }
        public float MasterPeak { get; set; }
    }
}
