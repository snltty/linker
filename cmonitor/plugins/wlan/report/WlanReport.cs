using cmonitor.client.report;
using cmonitor.config;

namespace cmonitor.plugins.wlan.report
{
    public sealed class WlanReport : IClientReport
    {
        public string Name => "Wlan";
        private readonly IWlan wlan;

        public WlanReport(Config config, IWlan wlan)
        {
            this.wlan = wlan;

            WlanTask();
        }

        public object GetReports(ReportType reportType)
        {
            return null;
        }

        private void WlanTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (wlan.Connected() == false)
                    {
                        var wafis = wlan.WlanEnums();
                        foreach (var wifi in wafis)
                        {
                            bool res = await wlan.WlanConnect(wifi);
                            if (res)
                            {
                                break;
                            }
                        }
                    }
                    await Task.Delay(3000);
                }
            });
        }
    }

    public sealed class WlanSetInfo
    {
        public string[] Names { get; set; }
        public bool Auto { get; set; }
    }

}
