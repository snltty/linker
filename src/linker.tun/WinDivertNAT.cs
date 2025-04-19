using linker.libs;
using linker.libs.timer;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace linker.tun
{
    /// <summary>
    /// 应用层简单SNAT
    /// 大概意思是
    /// 1，收到【客户端A】的数据包，10.18.18.23(客户端A的虚拟网卡IP)->192.168.56.6(局域网IP)
    /// 2，改为 192.168.56.2(本机IP)->192.168.56.6(局域网IP)
    /// 3，回来是 192.168.56.6(局域网IP)->192.168.56.2(本机IP)
    /// 4，改为 192.168.56.6(局域网IP)->10.18.18.23(客户端A的虚拟网卡IP)
    /// 5，回到客户端A，就完成了NAT
    /// </summary>
    public sealed class WinDivertNAT
    {
        /// <summary>
        /// 驱动
        /// </summary>
        WinDivert winDivert;
        /// <summary>
        /// 源
        /// </summary>
        AddrInfo src;
        /// <summary>
        /// 目标
        /// </summary>
        AddrInfo[] dsts;

        IPAddress interfaceIp;
        NetworkIPv4Addr interfaceAddr;

        public bool Running => winDivert != null;

        private CancellationTokenSource cts;
        private ConcurrentDictionary<(uint src, ushort srcPort, uint dst, ushort dstPort, ProtocolType pro), NatMapInfo> natMap = new ConcurrentDictionary<(uint src, ushort srcPort, uint dst, ushort dstPort, ProtocolType pro), NatMapInfo>();

        public WinDivertNAT(AddrInfo src, AddrInfo[] dsts, IPAddress interfaceIp)
        {
            this.src = src;
            this.dsts = dsts;
            this.interfaceIp = interfaceIp;
        }

        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool Setup(out string error)
        {
            error = string.Empty;

            if (OperatingSystem.IsWindows() == false || (RuntimeInformation.ProcessArchitecture != Architecture.X86 && RuntimeInformation.ProcessArchitecture != Architecture.X64))
            {
                error = "only windows x64,x86";
                return false;
            }
            if (src == null || dsts == null || dsts.Length == 0)
            {
                error = "src is null, or dsts empty";
                return false;
            }
            try
            {
                interfaceAddr = IPv4Addr.Parse(interfaceIp.ToString());
                winDivert = new WinDivert(BuildFilter(), WinDivert.Layer.Network, 0, 0);

                Recv();
                ClearTask();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            return false;
        }
        private string BuildFilter()
        {
            IEnumerable<string> ipRanges = dsts.Select(c => $"(ip.SrcAddr >= {c.NetworkIP} and ip.SrcAddr <= {c.BroadcastIP})");
            return $"inbound and ({string.Join(" or ", ipRanges)})";
        }
        private void Recv()
        {
            cts = new CancellationTokenSource();
            TimerHelper.Async(() =>
            {
                Memory<byte> packet = new Memory<byte>(new byte[10 * WinDivert.MTUMax]);
                Memory<WinDivertAddress> abuf = new Memory<WinDivertAddress>(new WinDivertAddress[10]);
                uint recvLen = 0, addrLen = 0;
                while (cts.IsCancellationRequested == false)
                {
                    try
                    {
                        (recvLen, addrLen) = winDivert.RecvEx(packet.Span, abuf.Span);

                        Memory<byte> recv = packet[..(int)recvLen];
                        Memory<WinDivertAddress> addr = abuf[..(int)addrLen];
                        foreach (var (i, p) in new WinDivertIndexedPacketParser(recv))
                        {
                            Recv(p, ref addr.Span[i]);
                        }
                        winDivert.SendEx(recv.Span, addr.Span);
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
                Dispose();
            });
        }

        /// <summary>
        /// 还原数据包
        /// </summary>
        /// <param name="p"></param>
        /// <param name="addr"></param>
        private unsafe void Recv(WinDivertParseResult p, ref WinDivertAddress addr)
        {
            fixed (byte* ptr = p.Packet.Span)
            {
                byte ipHeaderLength = (byte)((*ptr & 0b1111) * 4);
                ProtocolType proto = (ProtocolType)p.IPv4Hdr->Protocol;
                bool result = (ProtocolType)p.IPv4Hdr->Protocol switch
                {
                    ProtocolType.Icmp => RecvIcmp(p, ptr),
                    ProtocolType.Tcp => RecvTcp(p, ptr),
                    ProtocolType.Udp => RecvUdp(p, ptr),
                    _ => false,
                };
                if (result)
                {
                    WinDivert.CalcChecksums(p.Packet.Span, ref addr, 0);
                }
            }
        }
        /// <summary>
        /// 注入数据包，让它直接走正确的网卡，路由到目的地
        /// </summary>
        /// <param name="buffer"></param>
        public unsafe bool Inject(ReadOnlyMemory<byte> buffer)
        {
            fixed (byte* ptr = buffer.Span)
            {
                foreach (var (i, p) in new WinDivertIndexedPacketParser(buffer))
                {
                    bool result = (ProtocolType)p.IPv4Hdr->Protocol switch
                    {
                        ProtocolType.Icmp => InjectIcmp(p, ptr),
                        ProtocolType.Tcp => InjectTcp(p, ptr),
                        ProtocolType.Udp => InjectUdp(p, ptr),
                        _ => false,
                    };
                    if (result == false) return false;

                    WinDivertAddress addr = new WinDivertAddress
                    {
                        Layer = WinDivert.Layer.Network,
                        Outbound = true,
                        IPv6 = false
                    };

                    WinDivert.CalcChecksums(p.Packet.Span, ref addr, 0);
                    winDivert.SendEx(p.Packet.Span, new ReadOnlySpan<WinDivertAddress>(ref addr));
                }
            }
            return true;
        }

        /// <summary>
        /// 注入ICMP
        /// </summary>
        /// <param name="p"></param>
        /// <param name="ptr"></param>
        /// <returns></returns>
        private unsafe bool InjectIcmp(WinDivertParseResult p, byte* ptr)
        {
            //只操作response 和 request
            if (p.ICMPv4Hdr->Type != 0 && p.ICMPv4Hdr->Type != 8) return false;

            //IP头长度
            byte ipHeaderLength = (byte)((p.Packet.Span[0] & 0b1111) * 4);
            //原标识符，两个字节
            byte* ptr0 = ptr + ipHeaderLength + 4;
            byte* ptr1 = ptr + ipHeaderLength + 5;

            //用源地址的第三个，第四个字节作为新的标识符
            byte identifier0 = p.Packet.Span[14];
            byte identifier1 = p.Packet.Span[15];

            //保存，源地址。标识符0，目的地址，标识符1，ICMP
            //取值，目的地址，标识符0，源地址，标识符1，ICMP
            //因为回来的数据包，地址交换了
            ValueTuple<uint, ushort, uint, ushort, ProtocolType> key = (interfaceAddr.Raw, identifier0, p.IPv4Hdr->DstAddr.Raw, identifier1, ProtocolType.Icmp);
            NatMapInfo natMapInfo = new NatMapInfo
            {
                SrcAddr = p.IPv4Hdr->SrcAddr,
                Identifier0 = *ptr0,
                Identifier1 = *ptr1,
                LastTime = Environment.TickCount64
            };
            natMap.AddOrUpdate(key, natMapInfo, (a, b) => natMapInfo);

            //改写为新的标识符
            *ptr0 = identifier0;
            *ptr1 = identifier1;
            //改写源地址为网卡地址
            p.IPv4Hdr->SrcAddr = interfaceAddr;

            return true;
        }
        /// <summary>
        /// 还原ICMP
        /// </summary>
        /// <param name="p"></param>
        /// <param name="ptr"></param>
        /// <returns></returns>
        private unsafe bool RecvIcmp(WinDivertParseResult p, byte* ptr)
        {
            //只操作response 和 request
            if (p.ICMPv4Hdr->Type != 0 && p.ICMPv4Hdr->Type != 8) return false;
            //IP头长度
            byte ipHeaderLength = (byte)((*ptr & 0b1111) * 4);

            //标识符，两个字节
            byte* ptr0 = ptr + ipHeaderLength + 4;
            byte* ptr1 = ptr + ipHeaderLength + 5;

            ValueTuple<uint, ushort, uint, ushort, ProtocolType> key = (p.IPv4Hdr->DstAddr.Raw, *ptr0, p.IPv4Hdr->SrcAddr.Raw, *ptr1, ProtocolType.Icmp);
            if (natMap.TryRemove(key, out NatMapInfo natMapInfo))
            {
                //改回原来的标识符
                *ptr0 = natMapInfo.Identifier0;
                *ptr1 = natMapInfo.Identifier1;
                p.IPv4Hdr->DstAddr = natMapInfo.SrcAddr;
                return true;

            }
            return false;
        }
        private unsafe bool InjectTcp(WinDivertParseResult p, byte* ptr)
        {
            return false;
            byte ipHeaderLength = (byte)((p.Packet.Span[0] & 0b1111) * 4);

            byte* ptr0 = ptr + ipHeaderLength + 4;
            byte* ptr1 = ptr + ipHeaderLength + 5;

            byte identifier0 = p.Packet.Span[14];
            byte identifier1 = p.Packet.Span[15];
            ValueTuple<uint, ushort, uint, ushort, ProtocolType> key = (interfaceAddr.Raw, identifier0, p.IPv4Hdr->DstAddr.Raw, identifier1, ProtocolType.Icmp);
            NatMapInfo natMapInfo = new NatMapInfo
            {
                SrcAddr = p.IPv4Hdr->SrcAddr,
                Identifier0 = *ptr0,
                Identifier1 = *ptr1,
                LastTime = Environment.TickCount64
            };
            natMap.AddOrUpdate(key, natMapInfo, (a, b) => natMapInfo);

            *ptr0 = identifier0;
            *ptr1 = identifier1;
            p.IPv4Hdr->SrcAddr = interfaceAddr;

            return true;
        }
        private unsafe bool RecvTcp(WinDivertParseResult p, byte* ptr)
        {
            return false;
        }
        private unsafe bool InjectUdp(WinDivertParseResult p, byte* ptr)
        {
            return false;
        }
        private unsafe bool RecvUdp(WinDivertParseResult p, byte* ptr) { return false; }

        /// <summary>
        /// 注销
        /// </summary>
        public void Dispose()
        {
            cts?.Cancel();

            winDivert?.Dispose();
            winDivert = null;

            natMap.Clear();
        }

        private void ClearTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                long now = Environment.TickCount64;
                foreach (var item in natMap.Where(c => now - c.Value.LastTime > 1 * 60 * 60).Select(c => c.Key).ToList())
                {
                    natMap.TryRemove(item, out _);
                }
            }, 5000);
        }
        public sealed class AddrInfo
        {
            public AddrInfo(IPAddress ip, byte prefixLength)
            {
                IP = ip;
                PrefixLength = prefixLength;

                PrefixValue = NetworkHelper.ToPrefixValue(PrefixLength);
                NetworkValue = NetworkHelper.ToNetworkValue(IP, PrefixLength);
                BroadcastValue = NetworkHelper.ToBroadcastValue(IP, PrefixLength);

                Addr = IPv4Addr.Parse(IP.ToString());
                NetworkAddr = IPv4Addr.Parse(NetworkHelper.ToIP(NetworkValue).ToString());

                NetworkIP = NetworkHelper.ToIP(NetworkValue);
                BroadcastIP = NetworkHelper.ToIP(BroadcastValue);
            }
            public IPAddress IP { get; }
            public byte PrefixLength { get; }

            public NetworkIPv4Addr Addr { get; private set; }
            public NetworkIPv4Addr NetworkAddr { get; private set; }

            public uint PrefixValue { get; private set; }
            public uint NetworkValue { get; private set; }
            public uint BroadcastValue { get; private set; }

            public IPAddress NetworkIP { get; private set; }
            public IPAddress BroadcastIP { get; private set; }
        }
        sealed class NatMapInfo
        {
            public NetworkIPv4Addr SrcAddr { get; set; }
            public byte Identifier0 { get; set; }
            public byte Identifier1 { get; set; }
            public long LastTime { get; set; } = Environment.TickCount64;
        }
    }
}
