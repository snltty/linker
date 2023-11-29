using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace cmonitor.server.client.reports.volume
{
    public sealed class VolumeWindows : IVolume
    {
        MMDeviceEnumerator deviceEnumerator;
        MMDevice defaultDevice;
        AudioMeterInformation information;
        public VolumeWindows(Config config)
        {
            if (config.IsCLient)
            {
                deviceEnumerator = new MMDeviceEnumerator();
                defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                information = defaultDevice.AudioMeterInformation;
            }
        }

        public float GetMasterPeak()
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

        public bool GetMute()
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
        public void SetMute(bool value)
        {
            try
            {
                defaultDevice.AudioEndpointVolume.Mute = value;
            }
            catch (Exception)
            {
            }
        }

        public float GetVolume()
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

        public void SetVolume(float value)
        {
            try
            {
                value = Math.Max(0, Math.Min(value, 1));
                defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = value;
            }
            catch (Exception)
            {
            }
        }


        public void Play(byte[] audioBytes)
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
    }
}
