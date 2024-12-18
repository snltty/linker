namespace linker.messenger.tunnel
{
    /// <summary>
    /// 打洞排除IP
    /// </summary>
    public sealed class TunnelExcludeIPTransfer
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
            return excludeIPs.SelectMany(c => c.Get()).ToList();
        }
    }
}
