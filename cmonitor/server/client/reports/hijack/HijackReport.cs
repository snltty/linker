using cmonitor.hijack;
using common.libs;

namespace cmonitor.server.client.reports.hijack
{
    internal sealed class HijackReport : IReport
    {
        public string Name => "Hijack";

        private readonly HijackEventHandler hijackEventHandler;
        private readonly HijackConfig hijackConfig;
        private ulong[] array = new ulong[3];
        private ulong[] lastArray = new ulong[3];
        private long ticks = DateTime.UtcNow.Ticks;

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

        public object GetReports(ReportType reportType)
        {
            array[0] = hijackEventHandler.UdpSend + hijackEventHandler.TcpSend;
            array[1] = hijackEventHandler.TcpReceive + hijackEventHandler.UdpReceive;
            ulong count = (ulong)(hijackConfig.AllowIPs.Length + hijackConfig.DeniedIPs.Length + hijackConfig.AllowDomains.Length + hijackConfig.DeniedDomains.Length + hijackConfig.AllowProcesss.Length + hijackConfig.DeniedProcesss.Length);
            array[2] = count;

            long _ticks = DateTime.UtcNow.Ticks;
            if (((_ticks - ticks) / TimeSpan.TicksPerMillisecond >= 300 && array.SequenceEqual(lastArray) == false))
            {
                ticks = _ticks;
                lastArray[0] = array[0];
                lastArray[1] = array[1];
                lastArray[2] = array[2];
                return array;
            }
            return null;
        }
    }
}
