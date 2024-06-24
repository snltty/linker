using linker.tunnel.wanport;
using linker.tunnel.transport;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace linker.tunnel.adapter
{
    public interface ITunnelAdapter
    {
        /// <summary>
        /// 本机局域网IP
        /// </summary>
        public IPAddress LocalIP { get; }

        /// <summary>
        /// ssl加密证书
        /// </summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// 获取外网端口协议列表
        /// </summary>
        /// <returns></returns>
        public List<TunnelWanPortInfo> GetTunnelWanPortCompacts();
        /// <summary>
        /// 保存外网端口协议列表
        /// </summary>
        /// <param name="compacts"></param>
        public void SetTunnelWanPortCompacts(List<TunnelWanPortInfo> compacts);

        /// <summary>
        /// 获取打洞协议列表
        /// </summary>
        /// <returns></returns>
        public List<TunnelTransportItemInfo> GetTunnelTransports();
        /// <summary>
        /// 保存打洞协议列表
        /// </summary>
        /// <param name="transports"></param>
        public void SetTunnelTransports(List<TunnelTransportItemInfo> transports);

        /// <summary>
        /// 获取本地网络信息
        /// </summary>
        /// <returns></returns>
        public NetworkInfo GetLocalConfig();
        /// <summary>
        /// 获取远端的外网信息，比如你是A，你要获取B的信息，可以在B调用  TunnelTransfer.GetWanPort() 发送回来
        /// </summary>
        /// <param name="remoteMachineId"></param>
        /// <returns></returns>
        public Task<TunnelTransportWanPortInfo> GetRemoteWanPort(string remoteMachineId);

        /// <summary>
        /// 发送开始打洞
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public Task<bool> SendConnectBegin(TunnelTransportInfo tunnelTransportInfo);
        /// <summary>
        /// 发送打洞失败
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public Task<bool> SendConnectFail(TunnelTransportInfo tunnelTransportInfo);
        /// <summary>
        /// 发送打洞成功
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public Task<bool> SendConnectSuccess(TunnelTransportInfo tunnelTransportInfo);
    }

    public sealed class NetworkInfo
    {
        /// <summary>
        /// 本机局域网IP列表
        /// </summary>
        public IPAddress[] LocalIps { get; set; }
        /// <summary>
        /// 本机与外网的距离，通过多少网关
        /// </summary>
        public int RouteLevel { get; set; }
        /// <summary>
        /// 本机名
        /// </summary>
        public string MachineId { get; set; }
    }
}
