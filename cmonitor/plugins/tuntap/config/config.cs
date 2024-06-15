using cmonitor.plugins.tuntap.config;
using System.Buffers.Binary;
using System.Net;

namespace cmonitor.plugins.tuntap.config
{
    public sealed class TuntapConfigInfo
    {
        public IPAddress ip = IPAddress.Any;
        public IPAddress IP
        {
            get => ip; set
            {
                ip = value;
                IpInt = BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes());
            }
        }
        public uint IpInt { get; private set; }


        public IPAddress[] LanIPs { get; set; } = Array.Empty<IPAddress>();
        public bool Running { get; set; }
    }
}


namespace cmonitor.client.config
{
    public sealed partial class RunningConfigInfo
    {
        public TuntapConfigInfo Tuntap { get; set; } = new TuntapConfigInfo();
    }
}