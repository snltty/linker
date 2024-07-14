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
        /// 缓冲区大小
        /// </summary>
        public byte BufferSize { get; set; } = 3;

        /// <summary>
        /// 局域网IP列表
        /// </summary>
        public IPAddress[] LanIPs { get; set; } = Array.Empty<IPAddress>();
        /// <summary>
        /// 局域网掩码列表，与IP列表一一对应
        /// </summary>
        public int[] Masks { get; set; } = Array.Empty<int>();
        /// <summary>
        /// 是否在运行中
        /// </summary>
        public bool Running { get; set; }
    }
}


namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 虚拟网卡配置
        /// </summary>
        public TuntapConfigInfo Tuntap { get; set; } = new TuntapConfigInfo();
    }
}