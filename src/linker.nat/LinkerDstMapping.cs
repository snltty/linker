using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Net;

namespace linker.nat
{
    /// <summary>
    /// 网段映射
    /// 一般来说，写入网卡前使用ToRealDst转换为真实IP，网卡读出后使用ToFakeDst转换回假IP回复给对方
    /// </summary>
    public sealed class LinkerDstMapping
    {
        private FrozenDictionary<uint, uint> mapDic = new Dictionary<uint, uint>().ToFrozenDictionary();
        private uint[] masks = [];
        private readonly ConcurrentDictionary<uint, uint> natDic = new ConcurrentDictionary<uint, uint>();

        private long natCount = 0;

        /// <summary>
        /// 设置映射目标
        /// </summary>
        /// <param name="maps"></param>
        public void SetDsts(DstMapInfo[] maps)
        {
            if (maps == null || maps.Length == 0)
            {
                mapDic = new Dictionary<uint, uint>().ToFrozenDictionary();
                masks = [];
                natDic.Clear();
                Interlocked.Exchange(ref natCount, 0);
                return;
            }

            mapDic = maps.ToFrozenDictionary(x => NetworkHelper.ToNetworkValue(x.FakeIP, x.PrefixLength), x => NetworkHelper.ToNetworkValue(x.RealIP, x.PrefixLength));
            masks = maps.Select(x => NetworkHelper.ToPrefixValue(x.PrefixLength)).ToArray();

        }

        /// <summary>
        /// 获取真实IP
        /// </summary>
        /// <param name="fakeIP">假IP</param>
        /// <returns></returns>
        public IPAddress GetRealDst(IPAddress fakeIP)
        {
            //映射表不为空
            if (masks.Length == 0 || mapDic.Count == 0) return fakeIP;

            uint fakeDist = NetworkHelper.ToValue(fakeIP);
            for (int i = 0; i < masks.Length; i++)
            {
                //目标IP网络号存在映射表中，找到映射后的真实网络号，替换网络号得到最终真实的IP
                if (mapDic.TryGetValue(fakeDist & masks[i], out uint realNetwork))
                {
                    uint realDist = realNetwork | (fakeDist & ~masks[i]);
                    return NetworkHelper.ToIP(realDist);
                }
            }
            return fakeIP;
        }

        /// <summary>
        /// 转换为假IP
        /// </summary>
        /// <param name="buffer">TCP/IP</param>
        public unsafe void ToFakeDst(ReadOnlyMemory<byte> buffer)
        {
            //映射表不为空
            if (natCount == 0) return;

            fixed (byte* ptr = buffer.Span)
            {
                MapPacket packet = new MapPacket(ptr);
                //只支持映射IPV4
                if (packet.Version != 4) return;

                if (natDic.TryGetValue(packet.SrcAddr, out uint fakeDist))
                {
                    packet.SrcAddr = fakeDist;
                }
            }

        }
        /// <summary>
        /// 转换为真IP
        /// </summary>
        /// <param name="buffer"></param>
        public unsafe void ToRealDst(ReadOnlyMemory<byte> buffer)
        {
            //映射表不为空
            if (masks.Length == 0 || mapDic.Count == 0) return;

            fixed (byte* ptr = buffer.Span)
            {
                MapPacket packet = new MapPacket(ptr);
                //只支持映射IPV4
                if (packet.Version != 4 || packet.DstAddrSpan.IsCast()) return;

                uint fakeDist = packet.DstAddr;
                for (int i = 0; i < masks.Length; i++)
                {
                    //目标IP网络号存在映射表中，找到映射后的真实网络号，替换网络号得到最终真实的IP
                    if (mapDic.TryGetValue(fakeDist & masks[i], out uint realNetwork))
                    {
                        uint realDist = realNetwork | (fakeDist & ~masks[i]);
                        if (packet.DstAddr != realDist)
                        {
                            packet.DstAddr = realDist;
                            if (natDic.TryGetValue(realDist, out uint value) == false || value != fakeDist)
                            {
                                natDic.AddOrUpdate(realDist, fakeDist, (a, b) => fakeDist);
                                Interlocked.Add(ref natCount, 1);
                            }
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 映射对象
        /// </summary>
        public sealed class DstMapInfo
        {
            /// <summary>
            /// 假IP
            /// </summary>
            public IPAddress FakeIP { get; set; }
            /// <summary>
            /// 真实IP
            /// </summary>
            public IPAddress RealIP { get; set; }
            /// <summary>
            /// 前缀
            /// </summary>
            public byte PrefixLength { get; set; }
        }

        readonly unsafe struct MapPacket
        {
            private readonly byte* ptr;

            public readonly byte Version => (byte)((*ptr >> 4) & 0b1111);
            public readonly uint SrcAddr
            {
                get
                {
                    return BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 12));
                }
                set
                {
                    *(uint*)(ptr + 12) = BinaryPrimitives.ReverseEndianness(value);
                }
            }
            public readonly uint DstAddr
            {
                get
                {
                    return BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 16));
                }
                set
                {
                    *(uint*)(ptr + 16) = BinaryPrimitives.ReverseEndianness(value);
                }
            }
            public ReadOnlySpan<byte> DstAddrSpan => new Span<byte>((ptr + 16), 4);

            public MapPacket(byte* ptr)
            {
                this.ptr = ptr;
            }
        }
    }
}
