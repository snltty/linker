using linker.libs.extends;
using linker.tunnel.connection;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.mesh
{
    public sealed class MeshSwapTransfer
    {
        public MeshSwapTransfer() { }

        public bool Swap(ITunnelConnection conn1, ITunnelConnection conn2, int limit = 0)
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
                SpeedLimit speedLimit = new SpeedLimit();
                speedLimit.SetLimit((uint)limit);

                conn1.BeginReceive(new TunnelCallback(conn2, speedLimit), null);
                conn2.BeginReceive(new TunnelCallback(conn1, speedLimit), null);
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
        private readonly SpeedLimit speedLimit;
        private readonly byte[] buffer = new byte[65535];

        public TunnelCallback(ITunnelConnection dst, SpeedLimit speedLimit)
        {
            this.dst = dst;
            this.speedLimit = speedLimit;
        }

        public async ValueTask Closed(ITunnelConnection connection, object state)
        {
            dst.Dispose();
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }

        public async ValueTask Receive(ITunnelConnection connection, ReadOnlyMemory<byte> data, object state)
        {
            if (speedLimit.NeedLimit())
            {
                int length = data.Length;
                speedLimit.TryLimit(ref length);
                while (length > 0)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                    speedLimit.TryLimit(ref length);
                }
            }

            TunnelPacket packet = new TunnelPacket(buffer, data, TunnelPacket.PacketFlagData, 0);
            await dst.SendAsync(packet.RawData).ConfigureAwait(false);
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

    public class SpeedLimit
    {
        private uint limit = 0;
        private double limitToken = 0;
        private double limitBucket = 0;
        private long limitTicks = Environment.TickCount64;

        public bool NeedLimit()
        {
            return limit > 0;
        }
        public void SetLimit(uint bytes)
        {
            //每s多少字节
            limit = bytes;
            //每ms多少字节
            limitToken = limit / 1000.0;
            //桶里有多少字节
            limitBucket = limit;
        }
        public bool TryLimit(ref int length)
        {
            //0不限速
            if (limit == 0) return true;

            lock (this)
            {
                long _limitTicks = Environment.TickCount64;
                //距离上次经过了多少ms
                long limitTicksTemp = _limitTicks - limitTicks;
                limitTicks = _limitTicks;
                //桶里增加多少字节
                limitBucket += limitTicksTemp * limitToken;
                //桶溢出了
                if (limitBucket > limit) limitBucket = limit;

                //能全部消耗调
                if (limitBucket >= length)
                {
                    limitBucket -= length;
                    length = 0;
                }
                else
                {
                    //只能消耗一部分
                    length -= (int)limitBucket;
                    limitBucket = 0;
                }
            }
            return true;
        }
        public bool TryLimitPacket(int length)
        {
            if (limit == 0) return true;

            lock (this)
            {
                long _limitTicks = Environment.TickCount64;
                long limitTicksTemp = _limitTicks - limitTicks;
                limitTicks = _limitTicks;
                limitBucket += limitTicksTemp * limitToken;
                if (limitBucket > limit) limitBucket = limit;

                if (limitBucket >= length)
                {
                    limitBucket -= length;
                    return true;
                }
            }
            return false;
        }
    }
}
