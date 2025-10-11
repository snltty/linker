using linker.tunnel.connection;
using linker.libs;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using linker.libs.extends;
using System.IO.Pipelines;

namespace linker.messenger.forward.proxy
{
    /// <summary>
    /// 端口转发代理
    /// </summary>
    public partial class ForwardProxy
    {
        private readonly NumberSpace ns = new NumberSpace();

        private ILinkerForwardHook[] hooks = [];

        /// <summary>
        /// 流量统计
        /// </summary>
        /// <param name="machineId"></param>
        /// <param name="target"></param>
        /// <param name="recvBytes"></param>
        /// <param name="sendtBytes"></param>
        public virtual void Add(string machineId, IPEndPoint target, long recvBytes, long sendtBytes)
        {
        }

        /// <summary>
        /// 开始转发
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="bufferSize"></param>
        private void Start(IPEndPoint ep, byte bufferSize)
        {
            StartTcp(ep, bufferSize);
            StartUdp(new IPEndPoint(ep.Address, LocalEndpoint.Port), bufferSize);
        }

        /// <summary>
        /// 隧道来数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        private async Task InputPacket(ITunnelConnection connection, ReadOnlyMemory<byte> memory)
        {
            using ForwardWritePacket packet = new ForwardWritePacket(memory);

            if (packet.ProtocolType == ProtocolType.Tcp)
            {
                switch (packet.Flag)
                {
                    case ForwardFlags.Psh:
                        await HandlePshTcp(connection, packet, memory).ConfigureAwait(false);
                        break;
                    case ForwardFlags.PshAck:
                        await HandlePshAckTcp(connection, packet, memory).ConfigureAwait(false);
                        break;
                    case ForwardFlags.Syn:
                        _ = HandleSynTcp(connection, packet, memory);
                        break;
                    case ForwardFlags.SynAck:
                        HandleSynAckTcp(connection, packet);
                        break;
                    case ForwardFlags.Rst:
                        HandleRstTcp(connection, packet);
                        break;
                    case ForwardFlags.RstAck:
                        HandleRstAckTcp(connection, packet);
                        break;
                    default:
                        break;
                }
            }
            else if (packet.ProtocolType == ProtocolType.Udp)
            {
                switch (packet.Flag)
                {
                    case ForwardFlags.Psh:
                        await HndlePshUdp(connection, packet, memory).ConfigureAwait(false);
                        break;
                    case ForwardFlags.PshAck:
                        await HndlePshAckUdp(connection, packet, memory).ConfigureAwait(false);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 添加钩子
        /// </summary>
        /// <param name="hooks"></param>
        public void AddHooks(List<ILinkerForwardHook> hooks)
        {
            List<ILinkerForwardHook> list = this.hooks.ToList();
            list.AddRange(hooks);

            this.hooks = list.Distinct().ToArray();
        }
        /// <summary>
        /// 连接钩子
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="ep"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        private bool HookConnect(string srcId, IPEndPoint ep, ProtocolType protocol)
        {
            foreach (var hook in hooks)
            {
                if (hook.Connect(srcId, ep, protocol) == false)
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 数据钩子
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool HookForward(AsyncUserToken token)
        {
            foreach (var hook in hooks)
            {
                if (hook.Forward(token) == false)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 关闭所有转发
        /// </summary>
        private void Stop()
        {
            StopTcp();
            StopUdp();
        }
        /// <summary>
        /// 关闭指定端口转发
        /// </summary>
        /// <param name="port"></param>
        private void Stop(int port)
        {
            StopTcp(port);
            StopUdp(port);
        }

        readonly unsafe struct ForwardWritePacket : IDisposable
        {
            private readonly byte* ptr;

            public readonly ForwardFlags Flag => (ForwardFlags)(*(ptr));
            public readonly ProtocolType ProtocolType => (ProtocolType)(*(ptr + 1));
            public readonly byte BufferSize => *(ptr + 2);

            public readonly ushort Port => *(ushort*)(ptr + 3);

            public readonly uint SrcAddr => *(uint*)(ptr + 5);
            public readonly ushort SrcPort => *(ushort*)(ptr + 9);
            public readonly uint DstAddr => *(uint*)(ptr + 11);
            public readonly ushort DstPort => *(ushort*)(ptr + 15);

            public readonly byte HeaderLength => (byte)(*(ptr + 17) - 4);

            public ForwardWritePacket(ReadOnlyMemory<byte> memory)
            {
                handle = memory.Pin();
                ptr = (byte*)handle.Pointer;
            }

            readonly MemoryHandle handle;
            public void Dispose()
            {
                handle.Dispose();
            }
        }
    }

    public interface ILinkerForwardHook
    {
        public bool Connect(string srcId, IPEndPoint ep, ProtocolType protocol);
        public bool Forward(AsyncUserToken token);
    }

    public enum ForwardFlags : byte
    {
        Fin = 0b00000001,
        Syn = 0b00000010,
        Rst = 0b00000100,
        Psh = 0b00001000,
        Ack = 0b00010000,
        Urg = 0b00100000,

        SynAck = Syn | Ack,
        PshAck = Psh | Ack,
        RstAck = Rst | Ack,
    }
    
    public unsafe sealed class ForwardReadPacket : IDisposable
    {
        private byte* ptr;

        public byte[] Buffer { get; set; }
        public int Offset { get; set; }
        public int Length
        {
            get
            {
                return Buffer.ToInt32() + 4;
            }
            set
            {
                (value - 4).ToBytes(Buffer.AsMemory());
            }
        }

        public ForwardFlags Flag
        {
            get
            {
                return (ForwardFlags)(*(ptr + 4));
            }
            set
            {
                *(ptr + 4) = (byte)value;
            }
        }
        public ProtocolType ProtocolType
        {
            get
            {
                return (ProtocolType)(*(ptr + 5));
            }
            set
            {
                *(ptr + 5) = (byte)value;
            }
        }
        public byte BufferSize
        {
            get
            {
                return *(ptr + 6);
            }
            set
            {
                *(ptr + 6) = value;
            }
        }

        public ushort Port
        {
            get
            {
                return *(ushort*)(ptr + 7);
            }
            set
            {
                *(ushort*)(ptr + 7) = value;
            }
        }

        public uint SrcAddr
        {
            get
            {
                return *(uint*)(ptr + 9);
            }
            set
            {
                *(uint*)(ptr + 9) = value;
            }
        }
        public ushort SrcPort
        {
            get
            {
                return *(ushort*)(ptr + 13);
            }
            set
            {
                *(ushort*)(ptr + 13) = value;
            }
        }
        public uint DstAddr
        {
            get
            {
                return *(uint*)(ptr + 15);
            }
            set
            {
                *(uint*)(ptr + 15) = value;
            }
        }
        public ushort DstPort
        {
            get
            {
                return *(ushort*)(ptr + 19);
            }
            set
            {
                *(ushort*)(ptr + 19) = value;
            }
        }

        public byte HeaderLength
        {
            get => *(ptr + 21);
            private set
            {
                *(ptr + 21) = value;
            }
        }

        public ForwardReadPacket(byte[] buffer)
        {
            Buffer = buffer;

            handle = buffer.AsMemory().Pin();
            ptr = (byte*)handle.Pointer;

            HeaderLength = 22;
        }

        MemoryHandle handle;
        public void Dispose()
        {
            handle.Dispose();
        }
    }

    public sealed class AsyncUserToken
    {
        public int ListenPort { get; set; }

        public TaskCompletionSource Tcs { get; set; }

        public Socket Socket { get; set; }
        public ITunnelConnection Connection { get; set; }
        public ForwardReadPacket ReadPacket { get; set; }

        public IPEndPoint IPEndPoint { get; set; }

        public LastTicksManager LastTicks { get; set; } = new LastTicksManager();
        public bool Timeout => LastTicks.Expired(60 * 1000);


        public Pipe Pipe { get; init; }
        private long received = 0;
        public long Received => received;

        public bool Sending { get; set; } = true;
        public bool Receiving { get; set; } = true;
        public void AddReceived(long value)
        {
            Interlocked.Add(ref received, value);
        }
        public bool NeedPause => Received > 512 * 1024 && Receiving;
        public bool NeedResume => Received < 128 * 1024 && Receiving == false;

        public void Disponse()
        {
            Pipe?.Writer.Complete();
            Pipe?.Reader.Complete();

            Socket?.SafeClose();
            Socket = null;

            ReadPacket?.Dispose();

            GC.Collect();
        }
    }
}
