using System.Net;

namespace linker.tun
{
    public interface ILinkerTunDevice
    {
        public string Name { get; }
        public bool Running { get; }

        public bool SetUp(IPAddress address, IPAddress gateway, byte prefixLength, out string error);
        public void Shutdown();

        public void AddRoute(LinkerTunDeviceRouteItem[] ips, IPAddress ip);
        public void DelRoute(LinkerTunDeviceRouteItem[] ip);

        public ReadOnlyMemory<byte> Read();
        public bool Write(ReadOnlyMemory<byte> buffer);
    }


    public interface ILinkerTunDeviceCallback
    {
        public void Callback(LinkerTunDevicPacket packet);
    }

    public struct LinkerTunDevicPacket
    {
        public byte Version;
        public ReadOnlyMemory<byte> SourceIPAddress;
        public ReadOnlyMemory<byte> DistIPAddress;
        public ReadOnlyMemory<byte> Packet;
    }

    public sealed class LinkerTunDeviceRouteItem
    {
        public IPAddress Address { get; }
        public byte Mask { get; }
    }

    public enum LinkerTunDeviceStatus
    {
        Normal = 0,
        Starting = 1,
        Running = 2
    }
}
