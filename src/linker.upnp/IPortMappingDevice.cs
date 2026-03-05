using System.Net;
using System.Net.Sockets;

namespace linker.upnp
{
    public enum DeviceType : byte
    {
        Upnp = 1,
        Pmp = 2,
        Pcp = 4,
        All = 255
    }
    public sealed class PortMappingInfo
    {
        /// <summary>
        /// 内网ip
        /// </summary>
        public IPAddress ClientIp { get; set; }
        /// <summary>
        /// 外网端口
        /// </summary>
        public int PublicPort { get; set; }
        /// <summary>
        /// 内网端口
        /// </summary>
        public int PrivatePort { get; set; }
        /// <summary>
        /// 协议
        /// </summary>
        public ProtocolType ProtocolType { get; set; }
        /// <summary>
        /// 启用
        /// </summary>
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 存活
        /// </summary>
        public int LeaseDuration { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public DeviceType DeviceType { get; set; } = DeviceType.All;

        public bool Deletable { get; set; } = true;

        public override string ToString()
        {
            return $"外网[*:{PublicPort}]->内网[{ClientIp}:{PrivatePort}]->启用:{Enabled}->协议:{ProtocolType}->存活:{LeaseDuration}->描述:{Description}";
        }
    }

    public interface IPortMappingDevice
    {
        /// <summary>
        /// 类型
        /// </summary>
        public DeviceType Type { get; }
        public IPAddress GatewayIp { get; set; }
        public IPAddress WanIp { get; set; }
        /// <summary>
        /// 获取设备所有映射
        /// </summary>
        /// <returns></returns>
        public Task<List<PortMappingInfo>> Get();
        public Task<PortMappingInfo> Get(int publicPort, ProtocolType protocolType);
        /// <summary>
        /// 添加映射
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public Task<bool> Add(PortMappingInfo mapping);
        /// <summary>
        /// 删除映射
        /// </summary>
        /// <param name="publicPort"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public Task<bool> Delete(int publicPort, ProtocolType protocol);
    }
}
