using linker.tunnel.transport;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace linker.tunnel
{
    public interface ITunnelMessengerAdapter
    {
        /// <summary>
        /// 连接id
        /// </summary>
        public string MachineId { get; }
        /// <summary>
        /// 配置的额外网络层级
        /// </summary>
        public int RouteLevelPlus { get; }
        /// <summary>
        /// 服务器地址
        /// </summary>
        public IPEndPoint ServerHost { get; }
        /// <summary>
        /// 加密密钥
        /// </summary>
        public X509Certificate Certificate { get; }

        /// <summary>
        /// 端口映射内外端口
        /// </summary>
        public int PortMapPrivate { get; }
        /// <summary>
        /// 端口映射外网端口
        /// </summary>
        public int PortMapPublic { get; }

        /// <summary>
        /// 获取远端的外网信息，比如你是A，你要获取B的信息，可以在B调用  TunnelTransfer.GetWanPort() 发送回来
        /// </summary>
        public Task<TunnelTransportWanPortInfo> GetRemoteWanPort(TunnelWanPortProtocolInfo info);

        /// <summary>
        /// 发送开始打洞
        /// </summary>
        public Task<bool> SendConnectBegin(TunnelTransportInfo info);
        /// <summary>
        /// 发送打洞失败
        /// </summary>
        public Task<bool> SendConnectFail(TunnelTransportInfo info);
        /// <summary>
        /// 发送打洞成功
        /// </summary>
        public Task<bool> SendConnectSuccess(TunnelTransportInfo info);

        /// <summary>
        /// 获取打洞排除IP
        /// </summary>
        /// <returns></returns>
        public List<TunnelExIPInfo> GetExcludeIps();

        /// <summary>
        /// 获取打洞协议列表
        /// </summary>
        /// <returns></returns>
        public Task<List<TunnelTransportItemInfo>> GetTunnelTransports(string machineId);
        /// <summary>
        /// 保存打洞协议列表
        /// </summary>
        /// <param name="transports"></param>
        public Task<bool> SetTunnelTransports(string machineid,List<TunnelTransportItemInfo> list);
    }
}
