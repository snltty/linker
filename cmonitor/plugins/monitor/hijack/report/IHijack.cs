namespace cmonitor.plugins.hijack.report
{
    public interface IHijack
    {
        public ulong UdpSend { get; }
        public ulong UdpReceive { get; }
        public ulong TcpSend { get; }
        public ulong TcpReceive { get; }

        public void Start();
        public void Stop();

        public void SetProcess(string[] white, string[] black);
        public void SetDomain(string[] white, string[] black, bool kill);
        public void SetIP(string[] white, string[] black);
        public void UpdateRules();
    }
}
