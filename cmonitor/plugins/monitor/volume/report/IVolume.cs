namespace cmonitor.plugins.volume.report
{
    public interface IVolume
    {
        public float GetVolume();
        public void SetVolume(float value);

        public float GetMasterPeak();

        public bool GetMute();
        public void SetMute(bool value);

        public void Play(byte[] audioBytes);
    }
}
