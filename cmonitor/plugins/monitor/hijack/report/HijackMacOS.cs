namespace cmonitor.plugins.hijack.report
{
    public sealed class HijackMacOS : IHijack
    {
        public ulong UdpSend => 0;

        public ulong UdpReceive => 0;

        public ulong TcpSend => 0;

        public ulong TcpReceive => 0;

        public void UpdateRules()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void SetProcess(string[] white, string[] black)
        {
        }

        public void SetDomain(string[] white, string[] black, bool kill)
        {
        }

        public void SetIP(string[] white, string[] black)
        {
        }
    }
}
