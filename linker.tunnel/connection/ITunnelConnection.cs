using System.Net;

namespace linker.tunnel.connection
{
    /// <summary>
    /// 隧道协议
    /// </summary>
    public enum TunnelProtocolType : byte
    {
        Tcp = 1,
        Udp = 2,
        Quic = 4,
    }
    /// <summary>
    /// 隧道模式
    /// </summary>
    public enum TunnelMode : byte
    {
        Client = 0,
        Server = 1,
    }
    /// <summary>
    /// 隧道类型
    /// </summary>
    public enum TunnelType : byte
    {
        P2P = 0,
        Relay = 1,
    }
    /// <summary>
    /// 隧道方向
    /// </summary>
    public enum TunnelDirection : byte
    {
        /// <summary>
        /// 正向连接
        /// </summary>
        Forward = 0,
        /// <summary>
        /// 反向连接
        /// </summary>
        Reverse = 1
    }

    public interface ITunnelConnectionReceiveCallback
    {
        public Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> data, object state);
        public Task Closed(ITunnelConnection connection, object state);
    }

    /// <summary>
    /// 隧道连接对象
    /// </summary>
    public interface ITunnelConnection
    {
        /// <summary>
        /// 对方id
        /// </summary>
        public string RemoteMachineId { get; }
        /// <summary>
        /// 对方名称
        /// </summary>
        public string RemoteMachineName { get; }
        /// <summary>
        /// 事务
        /// </summary>
        public string TransactionId { get; }
        /// <summary>
        /// 协议
        /// </summary>
        public string TransportName { get; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Label { get; }
        /// <summary>
        /// 隧道模式
        /// </summary>
        public TunnelMode Mode { get; }
        /// <summary>
        /// 隧道类型
        /// </summary>
        public TunnelType Type { get; }
        /// <summary>
        /// 协议
        /// </summary>
        public TunnelProtocolType ProtocolType { get; }
        /// <summary>
        /// 隧道方向
        /// </summary>
        public TunnelDirection Direction { get; }
        /// <summary>
        /// 对方IP
        /// </summary>
        public IPEndPoint IPEndPoint { get; }

        /// <summary>
        /// 是否SSL
        /// </summary>
        public bool SSL { get; }

        /// <summary>
        /// 缓冲区大小
        /// </summary>
        public byte BufferSize { get; } 

        /// <summary>
        /// 已连接
        /// </summary>
        public bool Connected { get; }
        /// <summary>
        /// 延迟
        /// </summary>
        public int Delay { get; }
        /// <summary>
        /// 已发送字节数
        /// </summary>
        public long SendBytes { get; }
        /// <summary>
        /// 已接受字节数
        /// </summary>
        public long ReceiveBytes { get; }

        /// <summary>
        /// 发送ping
        /// </summary>
        /// <returns></returns>
        public Task SendPing();
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ValueTask<bool> SendAsync(ReadOnlyMemory<byte> data);
        /// <summary>
        /// 开始接收数据
        /// </summary>
        /// <param name="callback">收到数据的回调</param>
        /// <param name="userToken">自定义数据，回调带上</param>
        /// <param name="framing">是否分包</param>
        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken, bool framing = true);

        /// <summary>
        /// 关闭隧道
        /// </summary>
        public void Dispose();

        public string ToString();
    }


}
