using linker.libs;
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
        /// <param name="packet">TCP/IP</param>
        public void ToFakeDst(ReadOnlyMemory<byte> packet)
        {
            //只支持映射IPV4
            if ((byte)(packet.Span[0] >> 4 & 0b1111) != 4) return;
            //映射表不为空
            if (natDic.IsEmpty) return;

            //源IP
            uint realDist = NetworkHelper.ToValue(packet.Span.Slice(12, 4));
            if (natDic.TryGetValue(realDist, out uint fakeDist))
            {
                //修改源IP
                ReWriteIP(packet, fakeDist, 12);
            }
        }
        /// <summary>
        /// 转换为真IP
        /// </summary>
        /// <param name="packet">TCP/IP</param>
        /// <param name="checksum">是否计算校验和，如果使用了应用层NAT，可以交给应用层NAT去计算校验和</param>
        public void ToRealDst(ReadOnlyMemory<byte> packet)
        {
            //只支持映射IPV4
            if ((byte)(packet.Span[0] >> 4 & 0b1111) != 4) return;
            //映射表不为空
            if (masks.Length == 0 || mapDic.Count == 0) return;
            //广播包
            if (packet.Span[19] == 255) return;

            uint fakeDist = NetworkHelper.ToValue(packet.Span.Slice(16, 4));

            for (int i = 0; i < masks.Length; i++)
            {
                //目标IP网络号存在映射表中，找到映射后的真实网络号，替换网络号得到最终真实的IP
                if (mapDic.TryGetValue(fakeDist & masks[i], out uint realNetwork))
                {
                    uint realDist = realNetwork | (fakeDist & ~masks[i]);
                    //修改目标IP
                    ReWriteIP(packet, realDist, 16);
                    if (natDic.TryGetValue(realDist, out uint value) == false || value != fakeDist)
                    {
                        natDic.AddOrUpdate(realDist, fakeDist, (a, b) => fakeDist);
                    }
                    break;
                }
            }
        }
        /// <summary>
        /// 写入新IP
        /// </summary>
        /// <param name="packet">IP包</param>
        /// <param name="newIP">大端IP</param>
        /// <param name="pos">写入位置，源12，目的16</param>
        private unsafe void ReWriteIP(ReadOnlyMemory<byte> packet, uint newIP, int pos)
        {
            fixed (byte* ptr = packet.Span)
            {
                //修改目标IP，需要小端写入，IP计算都是按大端的，操作是小端的，所以转换一下
                *(uint*)(ptr + pos) = BinaryPrimitives.ReverseEndianness(newIP);
                //清空校验和，等待重新计算
                *(ushort*)(ptr + 10) = 0;
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
    }
}
