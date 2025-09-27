using linker.libs;
using linker.nat;
using linker.tun.device;
using System.Buffers.Binary;
using System.Net;

namespace linker.tun
{
    /// <summary>
    /// windows要放一个wintun.dll到根目录
    /// </summary>
    internal class Program
    {
        public static LinkerTunDeviceAdapter linkerTunDeviceAdapter;
        static void Main(string[] args)
        {
            linkerTunDeviceAdapter = new LinkerTunDeviceAdapter();
            linkerTunDeviceAdapter.Initialize(new LinkerTunDeviceCallbackICMP());
            linkerTunDeviceAdapter.Setup(new LinkerTunDeviceSetupInfo { Name = "linker0", Address = IPAddress.Parse("192.168.55.2"), PrefixLength = 24, Mtu = 1420 });

            if (string.IsNullOrWhiteSpace(linkerTunDeviceAdapter.SetupError))
            {
                Console.WriteLine(linkerTunDeviceAdapter.SetupError);
            }
            Console.ReadLine();
        }
    }

    public sealed class LinkerTunDeviceCallbackTCPUDP : ILinkerTunDeviceCallback
    {
        public async Task Callback(LinkerTunDevicPacket packet)
        {
            TCPUDPRead(packet);
            await Task.CompletedTask.ConfigureAwait(false);
        }
        public async Task Callback(LinkerSrcProxyReadPacket packet)
        {
        }
        public bool Callback(uint ip) { return true; }
        private unsafe void TCPUDPRead(LinkerTunDevicPacket packet)
        {
            if (packet.Version != 4) return;

            Memory<byte> writableMemory = packet.Buffer.AsMemory(packet.Offset + 4, packet.Length);
            fixed (byte* ptr = writableMemory.Span)
            {
                Console.WriteLine($"IPv{packet.Version} {ptr[9]}");
                //6tcp  17udp
                if (ptr[9] == 6 || ptr[9] == 17)
                {
                    IPAddress sourceIP = new IPAddress(packet.SrcIp.Span);
                    IPAddress distIP = new IPAddress(packet.DstIp.Span);

                    ushort sourcePort = *(ushort*)(ptr + 20);
                    ushort distPort = *(ushort*)(ptr + 22);

                    Console.WriteLine($"IPv{packet.Version}:[{(ptr[9] == 6 ? "TCP" : "UDP")}]  {new IPEndPoint(sourceIP, sourcePort)}->{new IPEndPoint(distIP, distPort)}");
                    //Program.linkerTunDeviceAdapter.Write(writableMemory);
                }
            }
        }
    }

    public sealed class LinkerTunDeviceCallbackICMP : ILinkerTunDeviceCallback
    {
        public async Task Callback(LinkerTunDevicPacket packet)
        {
            ICMPAnswer(packet);
            await Task.CompletedTask.ConfigureAwait(false);
        }
        public async Task Callback(LinkerSrcProxyReadPacket packet)
        {
        }
        public bool Callback(uint ip) { return true; }
        private unsafe void ICMPAnswer(LinkerTunDevicPacket packet)
        {
            Memory<byte> writableMemory = packet.Buffer.AsMemory(packet.Offset + 4, packet.Length);
            fixed (byte* ptr = writableMemory.Span)
            {

                //icmp && request
                if (ptr[9] == 1 && ptr[20] == 8)
                {
                    Console.WriteLine($"ICMP to {new IPAddress(writableMemory.Span.Slice(16, 4))}");

                    uint dist = BinaryPrimitives.ReadUInt32LittleEndian(writableMemory.Span.Slice(16, 4));


                    //目的地址变源地址，
                    *(uint*)(ptr + 16) = *(uint*)(ptr + 12);
                    //假装是网关回复的
                    *(uint*)(ptr + 12) = dist;
                    //response
                    *(ushort*)(ptr + 20) = 0;

                    ChecksumHelper.Checksum(ptr);
                    Program.linkerTunDeviceAdapter.Write(string.Empty, writableMemory);
                }
            }
        }
    }
}
