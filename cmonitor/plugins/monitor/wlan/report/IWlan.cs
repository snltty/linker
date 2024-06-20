namespace cmonitor.plugins.wlan.report
{
    public interface IWlan
    {
        public void Init();

        public List<string> Enums();
        public Task<bool> Connect();
    }


}
