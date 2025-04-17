using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static linker.libs.winapis.SECUR32;
using static System.Collections.Specialized.BitVector32;

namespace linker.tun
{
    /// <summary>
    /// 应用层NAT
    /// </summary>
    internal sealed class WinDivertNAT
    {
        public WinDivertNAT()
        {
            /*
            winDivert = new WinDivert("inbound and (( ip.SrcAddr == 10.18.18.0/24 and ip.DstAddr == 192.168.56.0/24) or ( ip.SrcAddr == 192.168.56.0/24))", WinDivert.Layer.Network, 0, WinDivert.Flag.Sniff);
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
            */
        }
       // NetworkIPv4Addr sourceAddr = IPv4Addr.Parse("10.18.18.23");
        private unsafe void ModifyPacket(WinDivertParseResult p, ref WinDivertAddress addr)
        {
            /*
            if (p.IPv4Hdr->SrcAddr == sourceAddr)
            {
                Console.WriteLine($"{p.IPv4Hdr->SrcAddr}->{p.IPv4Hdr->DstAddr}");
            }*/
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
    }
}
