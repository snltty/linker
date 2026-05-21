using linker.libs.extends;
using linker.tunnel.connection;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace linker.messenger.pcp
{
    public sealed class SwapTransfer
    {
        public SwapTransfer() { }

        public bool Swap(ITunnelConnection conn1, ITunnelConnection conn2)
        {
            if (conn1 == null || conn1 == null)
            {
                return false;
            }
            /*
            if(conn1.ProtocolType == conn2.ProtocolType)
            {
                IRevSender revsender1 = GetRevSender(conn1);
                IRevSender revsender2 = GetRevSender(conn2);
                Task.WhenAny(
                    SwapAsync(revsender1, revsender2),
                    SwapAsync(revsender2, revsender1)
                ).ConfigureAwait(false);
            }
            else
            */
            {
                conn1.BeginReceive(new TunnelCallback(conn2), null);
                conn2.BeginReceive(new TunnelCallback(conn1), null);
            }

            return true;
        }
        private async Task SwapAsync(IRevSender src, IRevSender dst)
        {
            Memory<byte> memory;
            while ((memory = await src.ReceiveAsync().ConfigureAwait(false)).Length > 0)
            {
                await dst.SendAsync(memory).ConfigureAwait(false);
            }
        }
        private IRevSender GetRevSender(ITunnelConnection connection)
        {
            if (connection is TunnelConnectionUdp udp)
            {
                return new RevSenderUdp(udp.UdpClient, connection.IPEndPoint);
            }
            if (connection is TunnelConnectionTcp tcp)
            {
                tcp.Stream.Dispose();
                return new RevSenderTcp(tcp.Socket);
            }
            return null;
        }
    }

    public sealed class TunnelCallback : ITunnelConnectionReceiveCallback
    {
        private readonly ITunnelConnection dst;
        public TunnelCallback(ITunnelConnection dst)
        {
            this.dst = dst;
        }

        public Task Closed(ITunnelConnection connection, object state)
        {
            dst.Dispose();
            return Task.CompletedTask;
        }

        public Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> data, object state)
        {
            return dst.SendAsync(data);
        }
    }

    public interface IRevSender
    {
        public ValueTask<Memory<byte>> ReceiveAsync();
        public ValueTask<int> SendAsync(Memory<byte> memory);
    }
    public sealed class RevSenderUdp : IRevSender
    {
        private readonly Socket socket;
        private readonly byte[] buffer = new byte[65535];
        private readonly IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        private readonly IPEndPoint target = new IPEndPoint(IPAddress.Any, 0);

        public RevSenderUdp(Socket socket, IPEndPoint target)
        {
            this.socket = socket;
            this.target = target;
        }

        public async ValueTask<Memory<byte>> ReceiveAsync()
        {
            var res = await socket.ReceiveFromAsync(buffer, SocketFlags.None, ep).ConfigureAwait(false);
            return buffer.AsMemory(0, res.ReceivedBytes);
        }

        public async ValueTask<int> SendAsync(Memory<byte> memory)
        {
            return await socket.SendToAsync(memory, SocketFlags.None, target).ConfigureAwait(false);
        }
    }
    public sealed class RevSenderTcp : IRevSender
    {
        private readonly Socket socket;
        private readonly byte[] buffer = new byte[8 * 1024];

        public RevSenderTcp(Socket socket)
        {
            this.socket = socket;
        }

        public async ValueTask<Memory<byte>> ReceiveAsync()
        {
            var res = await socket.ReceiveAsync(buffer, SocketFlags.None).ConfigureAwait(false);
            return buffer.AsMemory(0, res);
        }

        public async ValueTask<int> SendAsync(Memory<byte> memory)
        {
            return await socket.SendAllAsync(memory).ConfigureAwait(false);
        }
    }
}
