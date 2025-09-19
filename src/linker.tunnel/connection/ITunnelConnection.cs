using linker.libs;
using System.Net;

namespace linker.tunnel.connection
{
    /// <summary>
    /// 隧道协议
    /// </summary>
    [Flags]
    public enum TunnelProtocolType : byte
    {
        None = 0,
        Tcp = 1,
        Udp = 2,
        Quic = 4,
        All = 255
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
        Node = 2,
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
        /// <summary>
        /// 收到隧道数据包
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="data"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> data, object state);
        /// <summary>
        /// 收到隧道关闭
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public Task Closed(ITunnelConnection connection, object state);
    }

    /// <summary>
    /// 隧道连接对象
    /// </summary>
    public interface ITunnelConnection:IDisposable
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
        /// 事务标签
        /// </summary>
        public string TransactionTag { get; }
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
        /// 中继节点ID
        /// </summary>
        public string NodeId { get; }

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
        /// 发送缓冲区剩余大小
        /// </summary>
        public long SendBufferRemaining { get; }
        /// <summary>
        /// 发送缓冲区剩余比例
        /// </summary>
        public long SendBufferFree { get; }

        /// <summary>
        /// 发送缓冲区剩余大小
        /// </summary>
        public long RecvBufferRemaining { get; }
        /// <summary>
        /// 发送缓冲区剩余比例
        /// </summary>
        public long RecvBufferFree { get; }

        /// <summary>
        /// 最后通信时间
        /// </summary>
        public LastTicksManager LastTicks { get; }

        /// <summary>
        /// 缓冲区
        /// </summary>
        public byte[] PacketBuffer { get; set; }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<bool> SendAsync(ReadOnlyMemory<byte> data);
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public Task<bool> SendAsync(byte[] buffer,int offset,int length);
        /// <summary>
        /// 开始接收数据
        /// </summary>
        /// <param name="callback">收到数据的回调</param>
        /// <param name="userToken">自定义数据，回调带上</param>
        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken);

        public string ToString();
        public bool Equals(ITunnelConnection connection);
    }


}
