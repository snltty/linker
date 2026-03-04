using System.Net.Sockets;

namespace linker.upnp
{
    public interface IPortMappingService
    {
        public DeviceType Type { get; }

        /// <summary>
        /// 发现
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<List<IPortMappingDevice>> Discovery(CancellationToken token);

        /// <summary>
        /// 获取本类型的所有已发现的设备
        /// </summary>
        /// <returns></returns>
        public List<IPortMappingDevice> GetDevices();

        /// <summary>
        /// 获取所有设备的所有映射信息
        /// </summary>
        /// <returns></returns>
        public Task<List<PortMappingInfo>> Get();
        /// <summary>
        /// 添加一条映射
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public Task Add(PortMappingInfo mapping);
        /// <summary>
        /// 删除一条映射
        /// </summary>
        /// <param name="publicPort"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public Task Delete(int publicPort, ProtocolType protocol);
    }

}
