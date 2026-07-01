using linker.libs;
using linker.libs.extends;
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
        Mesh = 2,
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
        public ValueTask<bool> Receive(ITunnelConnection connection, ReadOnlyMemory<byte> data, object state);
        /// <summary>
        /// 收到隧道关闭
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public ValueTask Closed(ITunnelConnection connection, object state);


    }

    /// <summary>
    /// 隧道连接对象
    /// </summary>
    public interface ITunnelConnection
    {
        /// <summary>
        /// 对方id
        /// </summary>
        public string RemoteMachineId { get; set; }
        /// <summary>
        /// 对方名称
        /// </summary>
        public string RemoteMachineName { get; set; }
        /// <summary>
        /// 事务
        /// </summary>
        public string TransactionId { get; set; }
        /// <summary>
        /// 配置
        /// </summary>
        public Dictionary<string, string> Configure { get; }
        /// <summary>
        /// 协议
        /// </summary>
        public string TransportName { get; set; }
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
        public TunnelType Type { get; set; }

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

        public int HashCode { get; }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ValueTask<bool> SendAsync(ReadOnlyMemory<byte> data);
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public ValueTask<bool> SendAsync(byte[] buffer, int offset, int length);
        /// <summary>
        /// 开始接收数据
        /// </summary>
        /// <param name="callback">收到数据的回调</param>
        /// <param name="userToken">自定义数据，回调带上</param>
        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken);

        public void Dispose();
        public string ToString();
        public bool Equals(ITunnelConnection connection);
    }

    public struct TunnelPacket
    {
        /// <summary>
        /// 头里的长度值，是Payload的长度
        /// </summary>
        public ushort Length { get; private set; }
        /// <summary>
        /// 数据类型
        /// </summary>
        public byte Flag { get; private set; }
        /// <summary>
        /// 保留
        /// </summary>
        public byte Rsv { get; private set; }

        /// <summary>
        /// 长度(2字节) + 标志(1字节) + 保留(1字节) + 数据
        /// </summary>
        public ReadOnlyMemory<byte> RawData { get; private set; }
        /// <summary>
        /// 标志(1字节) + 保留(1字节) + 数据
        /// </summary>
        public ReadOnlyMemory<byte> Payload { get; private set; }
        /// <summary>
        /// 数据
        /// </summary>
        public ReadOnlyMemory<byte> PayloadData { get; private set; }


        public const int PacketHeaderSize = PacketLengthSize + PacketFlagSize;
        public const int PacketLengthSize = 2;
        public const int PacketFlagSize = 2;

        public const byte PacketFlagData = 0;
        public const byte PacketFlagPing = 1;
        public const byte PacketFlagPong = 2;
        public const byte PacketFlagFin = 4;

        public static int ReadLength(ReadOnlyMemory<byte> buffer) => buffer.ToUInt16();
        public static void WriteLength(int length, ReadOnlyMemory<byte> buffer)
        {
            ((ushort)length).ToBytes(buffer);
        }

        public TunnelPacket(Memory<byte> dst, ReadOnlyMemory<byte> payloadData, byte flag, byte rsv = 0)
        {
            ((ushort)(payloadData.Length + PacketFlagSize)).ToBytes(dst);
            dst.Span[2] = flag;
            dst.Span[3] = rsv;
            payloadData.CopyTo(dst.Slice(PacketHeaderSize));

            RawData = dst.Slice(0, payloadData.Length + PacketHeaderSize);
            Payload = dst.Slice(PacketLengthSize, payloadData.Length + PacketFlagSize);
            PayloadData = dst.Slice(PacketHeaderSize, payloadData.Length);
        }

        public TunnelPacket(ReadOnlyMemory<byte> buffer, bool withLengthPrefix = true)
        {
            if (withLengthPrefix)
            {
                Length = buffer.ToUInt16();
                Flag = buffer.Span[2];
                Rsv = buffer.Span[3];
                RawData = buffer;
                Payload = buffer.Slice(PacketLengthSize);
                PayloadData = buffer.Slice(PacketHeaderSize);
            }
            else
            {
                Flag = buffer.Span[0];
                Rsv = buffer.Span[1];
                RawData = buffer;
                Payload = buffer;
                PayloadData = buffer.Slice(PacketFlagSize);
            }
        }


    }
}
