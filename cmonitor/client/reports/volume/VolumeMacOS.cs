namespace cmonitor.client.reports.volume
{
    public sealed class VolumeMacOS : IVolume
    {
        public float GetMasterPeak()
        {
            return 0;
        }

        public bool GetMute()
        {
            return false;
        }

        public float GetVolume()
        {
            return 0;
        }

        public void Play(byte[] audioBytes)
        {
        }

        public void SetMute(bool value)
        {
        }

        public void SetVolume(float value)
        {
        }
    }
}
