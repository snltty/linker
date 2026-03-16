using linker.libs.extends;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace linker.messenger.tunnel.server
{
    /*
     *  无NAT
     *      内网：192.168.1.10:1234  →  NAT映射 →  公网：8.8.8.8:1234 -> 1.1.1.1:80
     *      任何外部主机都可以向 8.8.8.8:1234 发送数据
     *  完全锥型 
     *      内网：192.168.1.10:1234  →  NAT映射 →  公网：8.8.8.8:5678 -> 1.1.1.1:80
     *      内网：192.168.1.10:1234  →  NAT映射 →  公网：8.8.8.8:5678 -> 2.2.2.2:80
     *      任何外部主机都可以向 8.8.8.8:5678 发送数据
     *  受限锥型
     *      内网：192.168.1.10:1234  →  NAT映射 →  公网：8.8.8.8:5678 -> 1.1.1.1:80
     *      内网：192.168.1.10:1234  →  NAT映射 →  公网：8.8.8.8:5678 -> 2.2.2.2:80
     *      1.1.1.1任何端口都可以向 8.8.8.8:5678 发送数据
     *      2.2.2.2任何端口都可以向 8.8.8.8:5678 发送数据
     *  端口受限锥型
     *      内网：192.168.1.10:1234  →  NAT映射 →  公网：8.8.8.8:5678 -> 1.1.1.1:80
     *      内网：192.168.1.10:1234  →  NAT映射 →  公网：8.8.8.8:5678 -> 2.2.2.2:80
     *      1.1.1.1:80可以向 8.8.8.8:5678 发
     *      2.2.2.2:80可以向 8.8.8.8:5678 发
     *  对称型
     *      内网：192.168.1.10:1234  →  NAT映射 →  公网：8.8.8.8:5678 -> 1.1.1.1:80
     *      内网：192.168.1.10:1234  →  NAT映射 →  公网：8.8.8.8:5679 -> 2.2.2.2:80
     *      1.1.1.1:80可以向 8.8.8.8:5678 发
     *      2.2.2.2:80可以向 8.8.8.8:5679 发
     */
    public sealed class StunResolver : IResolver
    {
        public byte Type => (byte)ResolverType.Stun;

        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            StunMessageType type = (StunMessageType)memory.Span[0];
            using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(256);
            switch (type)
            {
                case StunMessageType.Get:
                    {
                        await SendAsync(socket, owner.Memory, ep).ConfigureAwait(false);
                    }
                    break;
                case StunMessageType.Send:
                    {
                        IPEndPoint source = GetEp(memory);
                        await SendAsync(socket, owner.Memory, source).ConfigureAwait(false);
                    }
                    break;
                case StunMessageType.SendAny:
                    {
                        using Socket socket1 = new Socket(ep.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                        socket1.WindowsUdpBug();
                        socket1.Bind(new IPEndPoint(IPAddress.Any, 0));
                        IPEndPoint source = GetEp(memory);
                        await SendAsync(socket1, owner.Memory, source).ConfigureAwait(false);
                        socket1.SafeClose();
                    }
                    break;
                case StunMessageType.Unknown:
                    break;
                default:
                    break;
            }
        }
        private async Task SendAsync(Socket socket, Memory<byte> memory, IPEndPoint ep)
        {
            for (int i = 0; i < 5; i++)
            {
                await socket.SendToAsync(BuildSendData(memory, ep), ep).ConfigureAwait(false);
            }
        }
        private IPEndPoint GetEp(Memory<byte> memory)
        {
            return new IPEndPoint(new IPAddress(memory.Span.Slice(1, 4)), BitConverter.ToUInt16(memory.Span.Slice(5)));
        }
        private Memory<byte> BuildSendData(Memory<byte> data, IPEndPoint ep)
        {
            ep = ep.MapToIPv4();

            Span<byte> span = data.Span;

            //给客户端返回他的IP+端口
            span[0] = (byte)ep.AddressFamily;
            ep.Address.TryWriteBytes(span.Slice(1), out int length);
            ((ushort)ep.Port).ToBytes(span.Slice(1 + length));

            //防止一些网关修改掉它的外网IP
            for (int i = 0; i < 1 + length + 2; i++)
            {
                span[i] = (byte)(span[i] ^ byte.MaxValue);
            }

            byte[] temp = Encoding.UTF8.GetBytes(Environment.TickCount64.ToString().Sha256().SubStr(0, new Random().Next(16, 32)));
            temp.AsSpan().CopyTo(span.Slice(1 + length + 2));

            return data.Slice(0, 1 + length + 2 + temp.Length);
        }

        public enum StunMessageType : byte
        {
            Get = 0x00,
            Send = 0x01,
            SendAny = 0x02,
            Unknown = 0xFF
        }
    }
}
