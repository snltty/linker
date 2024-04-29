namespace cmonitor.plugins.wlan.report
{
    public class WlanLinux : IWlan
    {
        public List<string> Enums()
        {
            return Array.Empty<string>().ToList();
        }

        public async Task<bool> Connect()
        {
            return await Task.FromResult(false);
        }

        public void Init()
        {
        }
    }
}
