using System.Net;

namespace linker.messenger.tunnel
{
    /// <summary>
    /// 打洞排除IP
    /// </summary>
    public sealed class TunnelExcludeIPTransfer
    {
        private List<ITunnelExcludeIP> excludeIPs = new List<ITunnelExcludeIP>();

        public TunnelExcludeIPTransfer()
        {
        }

        /// <summary>
        /// 加载排除IP的实现类
        /// </summary>
        /// <param name="list"></param>
        public void LoadTunnelExcludeIPs(List<ITunnelExcludeIP> list)
        {
            excludeIPs = list;
        }

        /// <summary>
        /// 获取所有实现类的排除IP
        /// </summary>
        /// <returns></returns>
        public List<IPAddress> Get()
        {
            return excludeIPs.SelectMany(c => c.Get()).ToList();
        }
    }
}
