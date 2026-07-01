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
            {
                SpeedLimit speedLimit = new SpeedLimit();
                speedLimit.SetLimit((uint)limit);

                conn1.BeginReceive(new TunnelCallback(conn2, speedLimit), null);
                conn2.BeginReceive(new TunnelCallback(conn1, speedLimit), null);
            }

            return true;
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

        public  ValueTask Closed(ITunnelConnection connection, object state)
        {
            dst.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> Receive(ITunnelConnection connection, ReadOnlyMemory<byte> data, object state)
        {
            if (speedLimit.NeedLimit() == false)
            {
                TunnelPacket packet = new TunnelPacket(buffer, data, TunnelPacket.PacketFlagData, 0);
                return dst.SendAsync(packet.RawData);
            }

            return ReceiveWithLimit(data);
        }

        private async ValueTask<bool> ReceiveWithLimit(ReadOnlyMemory<byte> data)
        {
            int length = data.Length;
            speedLimit.TryLimit(ref length);

            while (length > 0)
            {
                await Task.Delay(10).ConfigureAwait(false);
                speedLimit.TryLimit(ref length);
            }

            TunnelPacket packet = new TunnelPacket(buffer, data, TunnelPacket.PacketFlagData, 0);
            return await dst.SendAsync(packet.RawData).ConfigureAwait(false);
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
