namespace cmonitor.plugins.wlan.report
{
    public interface IWlan
    {
        public List<string> WlanEnums();
        public Task<bool> WlanConnect(string name);

        public bool Connected();
    }


}
