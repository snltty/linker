using linker.plugins.tuntap.config;
using System.Buffers.Binary;
using System.Net;

namespace linker.plugins.tuntap.config
{
    public sealed class TuntapConfigInfo
    {
        private IPAddress ip = IPAddress.Any;
        /// <summary>
        /// 网卡IP
        /// </summary>
        public IPAddress IP
        {
            get => ip; set
            {
                ip = value;
                IpInt = BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes());
            }
        }
        public uint IpInt { get; private set; }


        /// <summary>
        /// 局域网IP列表
        /// </summary>
        public IPAddress[] LanIPs { get; set; } = Array.Empty<IPAddress>();
        public bool Running { get; set; }
    }
}


namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        public TuntapConfigInfo Tuntap { get; set; } = new TuntapConfigInfo();
    }
}