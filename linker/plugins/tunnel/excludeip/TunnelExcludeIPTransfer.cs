using linker.client.config;

namespace linker.plugins.tunnel.excludeip
{
    public sealed partial class TunnelExcludeIPTransfer
    {
        private List<ITunnelExcludeIP> excludeIPs;

        public TunnelExcludeIPTransfer()
        {
        }

        public void LoadTunnelExcludeIPs(List<ITunnelExcludeIP> list)
        {
            excludeIPs = list;
           
        }

        public List<ExcludeIPItem> Get()
        {
            List<ExcludeIPItem> result = new List<ExcludeIPItem>();
            foreach (var item in excludeIPs)
            {
                var ips = item.Get();
                if (ips != null && ips.Length > 0)
                {
                    result.AddRange(ips);
                }
            }
            return result;
        }
    }
}
