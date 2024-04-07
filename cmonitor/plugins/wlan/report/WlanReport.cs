using cmonitor.client.report;
using cmonitor.config;
using common.libs;

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
            Logger.Instance.Warning($"network task started");
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (wlan.Connected() == false)
                        {
                            Logger.Instance.Warning($"network offline  reconnect it~");
                            var wafis = wlan.WlanEnums();
                            foreach (var wifi in wafis)
                            {
                                Logger.Instance.Warning($"network offline  reconnect {wifi}~");
                                bool res = await wlan.WlanConnect(wifi);
                                if (res)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex);
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
