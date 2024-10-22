using linker.libs;
using linker.tunnel.adapter;
using System.Net;

namespace linker.tunnel.wanport
{
    /// <summary>
    /// 外网端口协议
    /// </summary>
    public sealed class TunnelWanPortTransfer
    {
        private List<ITunnelWanPortProtocol> tunnelWanPorts = new List<ITunnelWanPortProtocol>();

        public List<TunnelWanPortProtocolType> Protocols => tunnelWanPorts.Select(p => p.ProtocolType).ToList();

        private readonly ITunnelAdapter tunnelAdapter;
        public TunnelWanPortTransfer(ITunnelAdapter tunnelAdapter)
        {
            this.tunnelAdapter = tunnelAdapter;
        }



        /// <summary>
        /// 加载所有外网端口协议
        /// </summary>
        /// <param name="assembs"></param>
        public void LoadTransports(List<ITunnelWanPortProtocol> tunnelWanPorts)
        {
            this.tunnelWanPorts = tunnelWanPorts;
            LoggerHelper.Instance.Info($"load tunnel wanport compacts:{string.Join(",", tunnelWanPorts.Select(c => c.Name))}");
        }


        /// <summary>
        /// 获取外网端口
        /// </summary>
        /// <param name="localIP">你的局域网IP</param>
        /// <returns></returns>
        public async Task<TunnelWanPortEndPoint> GetWanPortAsync(IPAddress localIP, TunnelWanPortProtocolType protocolType)
        {
            var tunnelWanPort = tunnelWanPorts.FirstOrDefault(c => c.ProtocolType == protocolType);
            if (tunnelWanPort == null) return null;
            try
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Debug($"get domain ip {tunnelAdapter.ServerHost}");
                IPEndPoint server = NetworkHelper.GetEndPoint(tunnelAdapter.ServerHost, 3478);
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Debug($"got domain ip {tunnelAdapter.ServerHost}->{server}");
                if (server == null) return null;
                TunnelWanPortEndPoint wanPort = await tunnelWanPort.GetAsync(localIP, server).ConfigureAwait(false);
                if (wanPort != null)
                {
                    wanPort.Local.Address = localIP;
                    return wanPort;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            return null;
        }
    }
}
