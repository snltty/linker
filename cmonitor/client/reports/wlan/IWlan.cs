namespace cmonitor.client.reports.wlan
{
    public interface IWlan
    {
        public List<string> WlanEnums();
        public Task<bool> WlanConnect(string name);
    }

    
}
