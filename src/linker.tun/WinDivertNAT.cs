using linker.libs;
using linker.libs.timer;
using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Net;
using System.Text;

namespace linker.tun
{
    /// <summary>
    /// 应用层NAT
    /// </summary>
    public sealed class WinDivertNAT
    {
        WinDivert winDivert;
        AddrInfo src;
        AddrInfo[] dsts;

        NetworkIPv4Addr srcAddr;

        //网络号对应网卡IP，用来替换源IP
        private FrozenDictionary<uint, NetworkIPv4Addr> sourceDic = new Dictionary<uint, NetworkIPv4Addr>().ToFrozenDictionary();
        public WinDivertNAT(AddrInfo src, AddrInfo[] dsts)
        {
            this.src = src;
            this.dsts = dsts;
            InitializeInterfaceIP();
        }

        public void Setup()
        {
            srcAddr = IPv4Addr.Parse(src.IP.ToString());

            StringBuilder sb = new StringBuilder("inbound");
            sb.Append($" and (ip.SrcAddr >= {src.NetworkIP} and ip.SrcAddr <= {src.BroadcastIP})");

            winDivert = new WinDivert(sb.ToString(), WinDivert.Layer.Network, 0, WinDivert.Flag.Sniff);
            var packet = new Memory<byte>(new byte[WinDivert.MTUMax]);
            var abuf = new Memory<WinDivertAddress>(new WinDivertAddress[1]);
            TimerHelper.Async(() =>
            {
                uint recvLen = 0, addrLen = 0;
                while (true)
                {
                    try
                    {
                        (recvLen, addrLen) = winDivert.RecvEx(packet.Span, abuf.Span);

                        var recv = packet[..(int)recvLen];
                        var addr = abuf[..(int)addrLen];
                        foreach (var (i, p) in new WinDivertIndexedPacketParser(recv))
                        {
                            ModifyPacket(p, ref addr.Span[i]);
                        }
                        //_ = winDivert.SendEx(recv.Span, addr.Span);
                    }
                    catch (Exception)
                    {
                        winDivert.Dispose();
                        break;
                    }

                }
            });
        }

        private unsafe void ModifyPacket(WinDivertParseResult p, ref WinDivertAddress addr)
        {

            if (NetworkHelper.ToNetworkValue(BinaryPrimitives.ReverseEndianness(p.IPv4Hdr->SrcAddr.Raw), src.PrefixValue) == src.NetworkValue)
            {
                Console.WriteLine($"{p.IPv4Hdr->SrcAddr}->{p.IPv4Hdr->DstAddr}================================");
            }
            //WinDivert.CalcChecksums(p.Packet.Span, ref addr, 0);
        }

        private void Inject(ReadOnlyMemory<byte> buffer)
        {
            /*
            if ((byte)(buffer.Span[0] >> 4 & 0b1111) == 4)
            {
                uint distIP = BinaryPrimitives.ReadUInt32BigEndian(buffer.Span.Slice(16, 4));
                if (distIP != address32)
                {
                    var addr = new WinDivertAddress
                    {
                        Layer = WinDivert.Layer.Network,
                        Outbound = true,
                        IPv6 = false
                    };
                    winDivert.SendEx(buffer.Span, new ReadOnlySpan<WinDivertAddress>(ref addr));
                    Console.WriteLine($"WinDivert sendto {string.Join(".", buffer.Span.Slice(16, 4).ToArray())}");
                    return true;
                }
            }
            */
        }

        private void InitializeInterfaceIP()
        {

        }

        public void Dispose()
        {
            winDivert?.Dispose();
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

                NetworkAddr = IPv4Addr.Parse(NetworkHelper.ToIP(NetworkValue).ToString());

                NetworkIP = NetworkHelper.ToIP(NetworkValue);
                BroadcastIP = NetworkHelper.ToIP(BroadcastValue);
            }
            public IPAddress IP { get; }
            public byte PrefixLength { get; }

            public NetworkIPv4Addr NetworkAddr { get; private set; }
            public uint PrefixValue { get; private set; }
            public uint NetworkValue { get; private set; }
            public uint BroadcastValue { get; private set; }

            public IPAddress NetworkIP { get; private set; }
            public IPAddress BroadcastIP { get; private set; }
        }
    }
}
