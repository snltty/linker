namespace cmonitor.client.reports.hijack
{
    public sealed class HijackMacOS : IHijack
    {
        public ulong UdpSend => 0;

        public ulong UdpReceive => 0;

        public ulong TcpSend => 0;

        public ulong TcpReceive => 0;

        public void SetRules()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}
