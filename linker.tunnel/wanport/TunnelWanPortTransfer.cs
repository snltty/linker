using linker.libs;
using System.Net;

namespace linker.tunnel.wanport
{
    /// <summary>
    /// 外网端口协议
    /// </summary>
    public sealed class TunnelWanPortTransfer
    {
        private List<ITunnelWanPortProtocol> tunnelWanPorts;

        public TunnelWanPortTransfer()
        {

        }

        /// <summary>
        /// 加载所有外网端口协议
        /// </summary>
        /// <param name="assembs"></param>
        public void Init(List<ITunnelWanPortProtocol> tunnelWanPorts)
        {
            this.tunnelWanPorts = tunnelWanPorts;
            LoggerHelper.Instance.Info($"load tunnel wanport compacts:{string.Join(",", tunnelWanPorts.Select(c => c.Name))}");
        }

        public List<TunnelWanPortTypeInfo> GetTypes()
        {
            List<TunnelWanPortTypeInfo> res = tunnelWanPorts.Select(c => new TunnelWanPortTypeInfo
            {
                Value = c.Type,
                Name = c.Type.ToString(),
                Protocols = tunnelWanPorts
                .Where(d => d.Type == c.Type).ToList().Select(d => d.ProtocolType).Distinct().ToDictionary(c => (int)c, d => d.ToString()),
            }).Distinct(new TunnelWanPortTypeInfoEqualityComparer()).ToList();
            return res;
        }

        /// <summary>
        /// 获取外网端口
        /// </summary>
        /// <param name="localIP">你的局域网IP</param>
        /// <returns></returns>
        public async Task<TunnelWanPortEndPoint> GetWanPortAsync(IPAddress localIP, TunnelWanPortInfo info)
        {
            if (info == null) return null;
            var tunnelWanPort = tunnelWanPorts.FirstOrDefault(c => c.Type == info.Type && c.ProtocolType == info.ProtocolType);
            if (tunnelWanPort == null) return null;
            try
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Debug($"get domain ip {info.Host}");
                IPEndPoint server = NetworkHelper.GetEndPoint(info.Host, 3478);
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
