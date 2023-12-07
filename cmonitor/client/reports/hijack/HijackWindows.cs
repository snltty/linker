using cmonitor.client.reports.hijack.hijack;

namespace cmonitor.client.reports.hijack
{
    public sealed class HijackWindows : IHijack
    {
        private readonly HijackEventHandler hijackEventHandler;
        private readonly HijackController hijackController;

        public HijackWindows(HijackConfig hijackConfig)
        {
            hijackEventHandler = new HijackEventHandler(hijackConfig);
            hijackController = new HijackController(hijackConfig, hijackEventHandler);
        }

        public ulong UdpSend => hijackEventHandler.UdpSend;
        public ulong UdpReceive => hijackEventHandler.UdpReceive;
        public ulong TcpSend => hijackEventHandler.TcpSend;
        public ulong TcpReceive => hijackEventHandler.TcpReceive;

        public void SetRules()
        {
            hijackController.SetRules();
        }

        public void Start()
        {
            hijackController.Start();
        }

        public void Stop()
        {
            hijackController.Stop();
        }
    }
}
