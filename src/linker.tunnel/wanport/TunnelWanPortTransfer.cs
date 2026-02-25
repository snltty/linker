using linker.libs;
using Mono.Nat;
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

        public void AddProtocol(ITunnelWanPortProtocol protocol)
        {
            if (!tunnelWanPorts.Any(c => c.ProtocolType == protocol.ProtocolType))
            {
                tunnelWanPorts.Add(protocol);
            }
        }

        /// <summary>
        /// 获取外网端口
        /// </summary>
        /// <param name="server"></param>
        /// <param name="protocolType"></param>
        /// <returns></returns>
        public async Task<TunnelWanPortEndPoint> GetWanPortAsync(IPEndPoint server, TunnelWanPortProtocolType protocolType)
        {
            var tunnelWanPort = tunnelWanPorts.FirstOrDefault(c => (c.ProtocolType & protocolType) == c.ProtocolType);
            if (tunnelWanPort == null)
            {
                LoggerHelper.Instance.Error($"wan port protocol <{protocolType}> not found");
                return null;
            }
            try
            {
                return await tunnelWanPort.GetAsync(server).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            return null;
        }

    }
}
