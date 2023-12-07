namespace cmonitor.client.reports.hijack
{
    public interface IHijack
    {
        public ulong UdpSend { get; }
        public ulong UdpReceive { get;  }
        public ulong TcpSend { get;  }
        public ulong TcpReceive { get;}

        public void Start();
        public void Stop();
        public void SetRules();
    }
}
