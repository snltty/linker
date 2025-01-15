using linker.tunnel.transport;

namespace linker.messenger.tunnel
{
    /// <summary>
    /// 打洞排除IP
    /// </summary>
    public sealed class TunnelClientExcludeIPTransfer
    {
        private List<ITunnelClientExcludeIP> excludeIPs = new List<ITunnelClientExcludeIP>();

        public TunnelClientExcludeIPTransfer()
        {
        }

        /// <summary>
        /// 加载排除IP的实现类
        /// </summary>
        /// <param name="list"></param>
        public void AddTunnelExcludeIPs(List<ITunnelClientExcludeIP> list)
        {
            excludeIPs = excludeIPs.Concat(list).ToList();
        }

        /// <summary>
        /// 获取所有实现类的排除IP
        /// </summary>
        /// <returns></returns>
        public List<TunnelExIPInfo> Get()
        {
            return excludeIPs.SelectMany(c => c.Get()).ToList();
        }
    }
}
