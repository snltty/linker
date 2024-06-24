using Linker.Tunnel.Adapter;
using Linker.Libs;
using System.Diagnostics;
using System.Net;

namespace Linker.Tunnel.WanPort
{
    /// <summary>
    /// 外网端口协议
    /// </summary>
    public sealed class TunnelWanPortTransfer
    {
        private List<ITunnelWanPort> tunnelWanPorts;
        private ITunnelAdapter tunnelAdapter;

        public TunnelWanPortTransfer()
        {
        }

        /// <summary>
        /// 加载所有外网端口协议
        /// </summary>
        /// <param name="assembs"></param>
        public void Init(ITunnelAdapter tunnelAdapter, List<ITunnelWanPort> tunnelWanPorts)
        {

            this.tunnelAdapter = tunnelAdapter;
            this.tunnelWanPorts = tunnelWanPorts;
            LoggerHelper.Instance.Warning($"load tunnel wanport compacts:{string.Join(",", tunnelWanPorts.Select(c => c.Name))}");
        }

        public List<TunnelWanPortTypeInfo> GetTypes()
        {
            return tunnelWanPorts.Select(c => new TunnelWanPortTypeInfo { Value = c.Type, Name = c.Type.ToString() }).Distinct(new TunnelWanPortTypeInfoEqualityComparer()).ToList();
        }

        /// <summary>
        /// 获取外网端口
        /// </summary>
        /// <param name="localIP">你的局域网IP</param>
        /// <returns></returns>
        public async Task<TunnelWanPortEndPoint> GetWanPortAsync(IPAddress localIP)
        {
            var tunnelWanPortItems = tunnelAdapter.GetTunnelWanPortCompacts();
            foreach (TunnelWanPortInfo item in tunnelWanPortItems)
            {
                if (item.Disabled || string.IsNullOrWhiteSpace(item.Host)) continue;
                ITunnelWanPort tunnelWanPort = tunnelWanPorts.FirstOrDefault(c => c.Type == item.Type);
                if (tunnelWanPort == null) continue;

                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    IPEndPoint server = NetworkHelper.GetEndPoint(item.Host, 3478);
                    sw.Stop();
                    if (sw.ElapsedMilliseconds > 1000)
                    {
                        LoggerHelper.Instance.Warning($"get domain ip time:{sw.ElapsedMilliseconds}ms");
                    }
                    TunnelWanPortEndPoint WanPort = await tunnelWanPort.GetAsync(server);
                    if (WanPort != null)
                    {
                        WanPort.Local.Address = localIP;
                        return WanPort;
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            return null;
        }
    }
}
