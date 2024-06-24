using Linker.Plugins.Tuntap.Config;
using System.Buffers.Binary;
using System.Net;

namespace Linker.Plugins.Tuntap.Config
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


namespace Linker.Client.Config
{
    public sealed partial class RunningConfigInfo
    {
        public TuntapConfigInfo Tuntap { get; set; } = new TuntapConfigInfo();
    }
}