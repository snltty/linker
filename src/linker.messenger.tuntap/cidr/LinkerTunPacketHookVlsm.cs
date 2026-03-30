using linker.libs;
using linker.libs.timer;
using linker.messenger.tuntap.client;
using linker.tun.hook;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace linker.messenger.tuntap.cidr
{
    internal sealed class LinkerTunPacketHookVlsm : ILinkerTunPacketHook
    {
        public string Name => "Vlsm";
        public LinkerTunPacketHookLevel ReadLevel => LinkerTunPacketHookLevel.Normal;
        public LinkerTunPacketHookLevel WriteLevel => LinkerTunPacketHookLevel.Normal;

        private uint ip, prefix;
        private TuntapVlsmStatus status;
        private bool enableSub = false;
        private readonly ConcurrentDictionary<string, (uint ip, uint prefix)> networkDic = new();
        private readonly ConcurrentDictionary<NatKey, NatCacheInfo> natDic = new(new NatKeyComparer());


        private readonly TuntapDecenter tuntapDecenter;
        public LinkerTunPacketHookVlsm(TuntapConfigTransfer tuntapConfigTransfer, TuntapDecenter tuntapDecenter)
        {
            this.tuntapDecenter = tuntapDecenter;
            tuntapDecenter.OnClear += Clear;
            tuntapDecenter.OnChanged += AddRoute;
            tuntapConfigTransfer.OnUpdate += () =>
            {
                ip = NetworkHelper.ToValue(tuntapConfigTransfer.Info.IP);
                prefix = NetworkHelper.ToPrefixValue(tuntapConfigTransfer.Info.PrefixLength);
                if (tuntapConfigTransfer.VlsmStatus != status)
                {
                    natDic.Clear();
                }
                status = tuntapConfigTransfer.VlsmStatus;
                enableSub = tuntapConfigTransfer.SubCount > 0;
            };
            ClearTask();
        }
        private void Clear()
        {
            networkDic.Clear();
            natDic.Clear();
        }
        private void AddRoute()
        {
            var oldKeys = networkDic.Keys.ToList();
            var newKeys = tuntapDecenter.Infos.Keys.ToList();

            foreach (var kvp in tuntapDecenter.Infos.Values)
            {
                (uint ip, uint prefix) value = (NetworkHelper.ToValue(kvp.IP), NetworkHelper.ToPrefixValue(kvp.PrefixLength));
                networkDic.AddOrUpdate(kvp.MachineId, value, (a, b) => value);
            }

            foreach (var key in oldKeys.Except(newKeys))
            {
                networkDic.TryRemove(key, out _);
            }
        }


        public unsafe (LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del) Read(ReadOnlyMemory<byte> packet)
        {
            if (status == TuntapVlsmStatus.OneWay && enableSub)
            {
                fixed (byte* ptr = packet.Span)
                {
                    byte version = (byte)((*ptr >> 4) & 0b1111);
                    ProtocolType protocol = (ProtocolType)(*(ptr + 9));

                    if (version == 4 && (protocol == ProtocolType.Udp || protocol == ProtocolType.Tcp))
                    {
                        uint srcIp = BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 12));
                        uint dstIp = BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 16));
                        ushort srcPort = BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + (*ptr & 0b1111) * 4)); ;
                        ushort dstPort = BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + (*ptr & 0b1111) * 4 + 2));

                        NatKey key = new NatKey(srcIp, srcPort, dstIp, dstPort);
                        if (natDic.TryGetValue(key, out NatCacheInfo cache) == false)
                        {
                            cache = new NatCacheInfo();
                            natDic.TryAdd(key, cache);
                        }
                        cache.LastTime = Environment.TickCount64;
                    }
                }
            }
            return (LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None);
        }

        public async ValueTask<(LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del)> WriteAsync(ReadOnlyMemory<byte> packet, uint originDstIp, string srcId)
        {
            if (enableSub == false)
            {
                return await ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None));
            }

            CheckResult checkResult = Check(packet, originDstIp, srcId);
            //相同网络号
            if (checkResult.Eq)
            {
                return await ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None));
            }
            if (status == TuntapVlsmStatus.TwoWay && (checkResult.SrcRange || checkResult.DstRange))
            {
                return await ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None));
            }
            if (status == TuntapVlsmStatus.OneWay)
            {
                //来方范围大
                if (checkResult.SrcRange)
                {
                    return await ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None));
                }
                //我方范围大，需要给它发过
                else if (checkResult.DstRange && natDic.TryGetValue(new NatKey(checkResult.DstIp, checkResult.DstPort, checkResult.SrcIp, checkResult.SrcPort), out NatCacheInfo cache))
                {
                    cache.LastTime = Environment.TickCount64;
                    return await ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None));
                }
            }
            return await ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.Next | LinkerTunPacketHookFlags.Write));
        }

        unsafe CheckResult Check(ReadOnlyMemory<byte> packet, uint originDstIp, string srcId)
        {
            fixed (byte* ptr = packet.Span)
            {
                byte version = (byte)((*ptr >> 4) & 0b1111);
                ProtocolType protocol = (ProtocolType)(*(ptr + 9));

                if (version == 4 && (protocol == ProtocolType.Udp || protocol == ProtocolType.Tcp))
                {
                    if (networkDic.TryGetValue(srcId, out (uint ip, uint prefix) value))
                    {
                        //对方网络
                        uint srcNetwork = NetworkHelper.ToNetworkValue(value.ip, value.prefix);
                        uint srcBroadcast = NetworkHelper.ToBroadcastValue(value.ip, value.prefix);
                        //我放网络
                        uint dstNetwork = NetworkHelper.ToNetworkValue(ip, prefix);
                        uint dstBroadcast = NetworkHelper.ToBroadcastValue(ip, prefix);

                        uint srcIp = BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 12));
                        uint dstIp = BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 16));
                        ushort srcPort = BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + (*ptr & 0b1111) * 4)); ;
                        ushort dstPort = BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + (*ptr & 0b1111) * 4 + 2));

                        bool srcRange = srcNetwork <= dstNetwork && srcBroadcast >= dstBroadcast;
                        bool dstRange = dstNetwork <= srcNetwork && dstBroadcast >= srcBroadcast;

                        return new CheckResult(srcNetwork == dstNetwork,
                            srcRange,
                            dstRange,
                            srcIp, srcPort, dstIp, dstPort
                        );
                    }
                }
            }
            return new CheckResult(true, true, true, 0, 0, 0, 0);
        }

        private void ClearTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                if (natDic.Count > 0)
                {
                    long now = Environment.TickCount64;
                    foreach (var item in natDic.Where(c => now - c.Value.LastTime > 60 * 60 * 2 * 1000).Select(c => c.Key).ToList())
                    {
                        natDic.TryRemove(item, out _);
                    }
                }
            }, 5000);
        }

        struct CheckResult
        {
            public readonly bool Eq = true;
            public readonly bool SrcRange = true;
            public readonly bool DstRange = true;
            public readonly uint SrcIp;
            public readonly ushort SrcPort;
            public readonly uint DstIp;
            public readonly ushort DstPort;

            public CheckResult(bool eq, bool srcRange, bool dstRange, uint srcIp, ushort srcPort, uint dstIp, ushort dstPort)
            {
                Eq = eq;
                SrcRange = srcRange;
                DstRange = dstRange;
                SrcIp = srcIp;
                SrcPort = srcPort;
                DstIp = dstIp;
                DstPort = dstPort;
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        readonly struct NatKey
        {
            public readonly uint SrcIp;
            public readonly ushort SrcPort;
            public readonly uint DstIp;
            public readonly ushort DstPort;

            public NatKey(uint srcIp, ushort srcPort, uint dstIp, ushort dstPort)
            {
                SrcIp = srcIp;
                SrcPort = srcPort;
                DstIp = dstIp;
                DstPort = dstPort;
            }
        }
        class NatKeyComparer : IEqualityComparer<NatKey>
        {
            public bool Equals(NatKey x, NatKey y) => x.SrcIp == y.SrcIp &&
                      x.SrcPort == y.SrcPort &&
                       x.DstIp == y.DstIp &&
                       x.DstPort == y.DstPort;

            public int GetHashCode(NatKey obj)
            {
                return HashCode.Combine(obj.SrcIp, obj.SrcPort, obj.DstIp, obj.DstPort);
            }
        }
        sealed class NatCacheInfo
        {
            public long LastTime { get; set; } = Environment.TickCount64;
        }
    }

}