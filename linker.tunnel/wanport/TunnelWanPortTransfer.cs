using linker.libs;
using System.Net;

namespace linker.tunnel.wanport
{
    /// <summary>
    /// 获取外网端口
    /// </summary>
    public sealed class TunnelWanPortTransfer
    {
        private List<ITunnelWanPortProtocol> tunnelWanPorts = new List<ITunnelWanPortProtocol>();

        public List<TunnelWanPortProtocolType> Protocols => tunnelWanPorts.Select(p => p.ProtocolType).ToList();

        public TunnelWanPortTransfer()
        {
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
        public async Task<TunnelWanPortEndPoint> GetWanPortAsync(IPEndPoint server,IPAddress localIP, TunnelWanPortProtocolType protocolType)
        {
            var tunnelWanPort = tunnelWanPorts.FirstOrDefault(c => c.ProtocolType == protocolType);
            if (tunnelWanPort == null) return null;
            try
            {
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
