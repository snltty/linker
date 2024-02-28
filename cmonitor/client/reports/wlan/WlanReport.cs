using common.libs.winapis;

namespace cmonitor.client.reports.wlan
{
    public sealed class WlanReport : IReport
    {
        public string Name => "Wlan";
        private readonly Config config;
        private readonly ClientConfig clientConfig;
        private readonly IWlan wlan;

        public WlanReport(Config config, ClientConfig clientConfig, IWlan wlan)
        {
            this.config = config;
            this.clientConfig = clientConfig;
            this.wlan = wlan;

            if (config.IsCLient)
            {
                WlanTask();
            }
        }

        public object GetReports(ReportType reportType)
        {
            return null;
        }

        public List<string> WlanEnums()
        {
            return wlan.WlanEnums();
        }
        public void WlanConnect(WlanSetInfo wlanSetInfo)
        {
            Task.Run(async () =>
            {
                foreach (var item in wlanSetInfo.Names)
                {
                    bool res = await wlan.WlanConnect(item);
                    if (res)
                    {
                        clientConfig.Wlan = item;
                        clientConfig.WlanAuto = wlanSetInfo.Auto;
                        break;
                    }
                }
            });
        }

        private void WlanTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (wlan.Connected() == false && /*string.IsNullOrWhiteSpace(clientConfig.Wlan) == false && clientConfig.WlanAuto*/)
                    {
                        var wafis = wlan.WlanEnums();
                        foreach (var wifi in wafis)
                        {
                            bool res = await wlan.WlanConnect(wifi/*clientConfig.Wlan*/);
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
