using System.Net;

namespace cmonitor.tunnel.connection
{
    public enum TunnelProtocolType : byte
    {
        Tcp = 1,
        Udp = 2,
        Quic = 4,
    }
    public enum TunnelMode : byte
    {
        Client = 0,
        Server = 1,
    }
    public enum TunnelType : byte
    {
        P2P = 0,
        Relay = 1,
    }
    public enum TunnelDirection : byte
    {
        Forward = 0,
        Reverse = 1
    }

    public interface ITunnelConnectionReceiveCallback
    {
        public Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> data, object state);
        public Task Closed(ITunnelConnection connection, object state);
    }

    public interface ITunnelConnection
    {
        public string RemoteMachineId { get; }
        public string RemoteMachineName { get; }
        public string TransactionId { get; }
        public string TransportName { get; }
        public string Label { get; }
        public TunnelMode Mode { get; }
        public TunnelType Type { get; }
        public TunnelProtocolType ProtocolType { get; }
        public TunnelDirection Direction { get; }
        public IPEndPoint IPEndPoint { get; }

        public bool SSL { get; }

        public bool Connected { get; }
        public int Delay { get; }
        public long SendBytes { get; }
        public long ReceiveBytes { get; }

        public Task SendPing();
        public ValueTask<bool> SendAsync(ReadOnlyMemory<byte> data);
        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken, bool framing = true);

        public void Dispose();

        public string ToString();
    }


}
