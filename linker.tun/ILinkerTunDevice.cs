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


    public sealed class LinkerTunDeviceRouteItem
    {
        public IPAddress Address { get; }
        public byte Mask { get; }
    }
}
