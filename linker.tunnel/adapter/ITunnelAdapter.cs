using linker.tunnel.wanport;
using linker.tunnel.transport;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace linker.tunnel.adapter
{
    public interface ITunnelAdapter
    {
        /// <summary>
        /// 本机局域网IP，当然你也可以使用0.0.0.0，但是使用局域网IP会提高打洞成功率
        /// </summary>
        public IPAddress LocalIP { get; }

        /// <summary>
        /// ssl加密证书，没有证书则无法加密通信
        /// </summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// 获取外网端口协议列表
        /// </summary>
        /// <returns></returns>
        public List<TunnelWanPortInfo> GetTunnelWanPortProtocols();
        /// <summary>
        /// 保存外网端口协议列表
        /// </summary>
        /// <param name="compacts"></param>
        public void SetTunnelWanPortProtocols(List<TunnelWanPortInfo> protocols, bool updateVersion);

        /// <summary>
        /// 获取打洞协议列表
        /// </summary>
        /// <returns></returns>
        public List<TunnelTransportItemInfo> GetTunnelTransports();
        /// <summary>
        /// 保存打洞协议列表
        /// </summary>
        /// <param name="transports"></param>
        public void SetTunnelTransports(List<TunnelTransportItemInfo> transports, bool updateVersion);

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
        public Task<TunnelTransportWanPortInfo> GetRemoteWanPort(TunnelWanPortProtocolInfo info);

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
        /// 本机局域网IP列表，可以通过NetworkHelper.GetRouteLevel 获取
        /// </summary>
        public IPAddress[] LocalIps { get; set; }
        /// <summary>
        /// 本机与外网的距离，通过多少网关，可以通过NetworkHelper.GetRouteLevel 获取
        /// </summary>
        public int RouteLevel { get; set; }
        /// <summary>
        /// 本机名
        /// </summary>
        public string MachineId { get; set; }
    }

    public sealed class PortMapInfo
    {
        public int WanPort { get; set; }
        public int LanPort { get; set; }
    }

    public sealed class TunnelWanPortProtocolInfo
    {
        /// <summary>
        /// 类别
        /// </summary>
        public TunnelWanPortType Type { get; set; }
        /// <summary>
        /// 协议
        /// </summary>
        public TunnelWanPortProtocolType ProtocolType { get; set; } = TunnelWanPortProtocolType.Udp;
        /// <summary>
        /// 对方id
        /// </summary>
        public string MachineId { get; set; }
    }
}
