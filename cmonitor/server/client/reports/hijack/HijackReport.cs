using cmonitor.hijack;
using common.libs;

namespace cmonitor.server.client.reports.hijack
{
    internal sealed class HijackReport : IReport
    {
        public string Name => "Hijack";

        private readonly HijackEventHandler hijackEventHandler;
        private readonly HijackConfig hijackConfig;
        ulong[] array = new ulong[3];
        ulong[] lastArray = new ulong[3];
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

        public object GetReports()
        {
            array[0] = hijackEventHandler.UdpSend + hijackEventHandler.TcpSend;
            array[1] = hijackEventHandler.TcpReceive + hijackEventHandler.UdpReceive;
            ulong count =(ulong)(hijackConfig.AllowIPs.Length + hijackConfig.DeniedIPs.Length + hijackConfig.AllowDomains.Length + hijackConfig.DeniedDomains.Length + hijackConfig.AllowProcesss.Length + hijackConfig.DeniedProcesss.Length);
            array[2] = count;

            if(array.SequenceEqual(lastArray) == false)
            {
                lastArray[0] = array[0];
                lastArray[1] = array[1];
                lastArray[2] = array[2];
                return array;
            }
            return null;
        }
    }
}
