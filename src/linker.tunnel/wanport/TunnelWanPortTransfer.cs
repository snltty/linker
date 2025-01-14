using linker.libs;
using System.Net;

namespace linker.tunnel.wanport
{
    /// <summary>
    /// 获取外网端口
    /// </summary>
    public sealed class TunnelWanPortTransfer
    {
        private List<ITunnelWanPortProtocol> tunnelWanPorts = new List<ITunnelWanPortProtocol> {
            new TunnelWanPortProtocolLinkerUdp(),
            new TunnelWanPortProtocolLinkerTcp()
        };

        public List<TunnelWanPortProtocolType> Protocols => tunnelWanPorts.Select(p => p.ProtocolType).ToList();

        public TunnelWanPortTransfer()
        {
        }

        /// <summary>
        /// 获取外网端口
        /// </summary>
        /// <param name="localIP">你的局域网IP</param>
        /// <returns></returns>
        public async Task<TunnelWanPortEndPoint> GetWanPortAsync(IPEndPoint server, TunnelWanPortProtocolType protocolType)
        {
            var tunnelWanPort = tunnelWanPorts.FirstOrDefault(c => c.ProtocolType == protocolType);
            if (tunnelWanPort == null) return null;
            try
            {
                TunnelWanPortEndPoint wanPort = await tunnelWanPort.GetAsync(server).ConfigureAwait(false);
                return wanPort;
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            return null;
        }

    }
}
