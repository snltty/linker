namespace cmonitor.client.reports.wlan
{
    public class WlanLinux : IWlan
    {
        public List<string> WlanEnums()
        {
            return Array.Empty<string>().ToList();
        }

        public async Task<bool> WlanConnect(string name)
        {
            return await Task.FromResult(false);
        }

        public bool Connected()
        {
            return false;
        }
    }
}
