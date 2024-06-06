using cmonitor.tunnel.compact;
using cmonitor.tunnel.transport;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace cmonitor.tunnel.adapter
{
    public interface ITunnelAdapter
    {

        public bool SSL { get; }

        /// <summary>
        /// 本机局域网IP，用于UDP打洞绑定
        /// </summary>
        public IPAddress LocalIP { get; }
        /// <summary>
        /// ssl加密证书
        /// </summary>
        public X509Certificate Certificate { get; }

        /// <summary>
        /// 获取外网端口协议列表
        /// </summary>
        /// <returns></returns>
        public List<TunnelCompactInfo> GetTunnelCompacts();
        /// <summary>
        /// 保存外网端口协议列表
        /// </summary>
        /// <param name="compacts"></param>
        public void SetTunnelCompacts(List<TunnelCompactInfo> compacts);

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
        /// 获取远端的外网信息，可以在远端调用  TunnelTransfer.GetExternalIP() 发送回来
        /// </summary>
        /// <param name="remoteMachineName"></param>
        /// <returns></returns>
        public Task<TunnelTransportExternalIPInfo> GetRemoteExternalIP(string remoteMachineName);

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
        public string MachineName { get; set; }
    }
}
