using linker.client.config;
using linker.config;
using linker.libs;
using linker.plugins.client;
using MemoryPack;

namespace linker.plugins.tunnel.excludeip
{
    public sealed partial class TunnelExcludeIPTransfer
    {
        private List<ITunnelExcludeIP> excludeIPs;

        private readonly RunningConfig running;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig fileConfig;

        public TunnelExcludeIPTransfer(RunningConfig running, ClientSignInState clientSignInState, FileConfig fileConfig)
        {
            this.running = running;
            this.clientSignInState = clientSignInState;
            this.fileConfig = fileConfig;
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
