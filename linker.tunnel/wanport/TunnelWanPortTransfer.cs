using linker.libs;
using linker.libs.extends;
using System.Net;
using System.Net.Sockets;

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
