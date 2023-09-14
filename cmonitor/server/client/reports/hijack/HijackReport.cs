using cmonitor.hijack;
using common.libs;

namespace cmonitor.server.client.reports.hijack
{
    internal sealed class HijackReport : IReport
    {
        public string Name => "Hijack";

        private readonly HijackEventHandler hijackEventHandler;
        private readonly HijackConfig hijackConfig;
        public HijackReport(HijackEventHandler hijackEventHandler, HijackController hijackController, HijackConfig hijackConfig)
        {
            this.hijackEventHandler = hijackEventHandler;
            this.hijackConfig = hijackConfig;
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    hijackController.Start();
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }
        }

        public Dictionary<string, object> GetReports()
        {
            ulong upload = hijackEventHandler.UdpSend + hijackEventHandler.TcpSend;
            ulong download = hijackEventHandler.TcpReceive + hijackEventHandler.UdpReceive;
            return new Dictionary<string, object> {
                { "Upload",upload},
                { "Download",download},
                { "Count",hijackConfig.AllowIPs.Length + hijackConfig.DeniedIPs.Length + hijackConfig.AllowDomains.Length+hijackConfig.DeniedDomains.Length + hijackConfig.AllowProcesss.Length+hijackConfig.DeniedProcesss.Length }
            };
        }
    }
}
