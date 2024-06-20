using cmonitor.client;
using cmonitor.client.report;
using common.libs;

namespace cmonitor.plugins.wlan.report
{
    public sealed class WlanReport : IClientReport
    {
        public string Name => "Wlan";
        private readonly IWlan wlan;

        public WlanReport(IWlan wlan, ClientSignInState clientSignInState)
        {
            this.wlan = wlan;
            clientSignInState.NetworkFirstEnabledHandle += () => { wlan.Init(); };

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
                        if (await wlan.Connect())
                        {
                            await Task.Delay(10000);
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
