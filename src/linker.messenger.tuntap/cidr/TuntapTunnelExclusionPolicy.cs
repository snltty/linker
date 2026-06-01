using linker.messenger.rpolicy;
using linker.messenger.tunnel.client;
using linker.messenger.tuntap.client;
using linker.tunnel;
using System.Net;

namespace linker.messenger.tuntap.cidr
{
    public sealed class TuntapTunnelExclusionPolicy : ITunnelExclusionPolicy
    {
        private readonly TuntapCidrDecenterManager tuntapCidrDecenterManager;
        private readonly TuntapDecenter tuntapDecenter;
        public TuntapTunnelExclusionPolicy(TuntapCidrDecenterManager tuntapCidrDecenterManager, TuntapDecenter tuntapDecenter)
        {
            this.tuntapCidrDecenterManager = tuntapCidrDecenterManager;
            this.tuntapDecenter = tuntapDecenter;
        }
        public List<TunnelExclusionPolicyInfo> Query()
        {
            //网卡IP和已添加到路由的局域网IP不参与打洞
            return tuntapDecenter.Infos.Values
                .Select(c => new TunnelExclusionPolicyInfo { IP = c.IP, PrefixLength = c.PrefixLength })
                .Concat(tuntapCidrDecenterManager.Routes.Select(c => new TunnelExclusionPolicyInfo { IP = c.Address, PrefixLength = c.PrefixLength })).ToList();
        }
    }

    public sealed class TuntapRouteExclusionPolicy : IRouteExclusionPolicy
    {
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        public TuntapRouteExclusionPolicy(TuntapConfigTransfer tuntapConfigTransfer)
        {
            this.tuntapConfigTransfer = tuntapConfigTransfer;
        }
        public List<IPAddress> Query()
        {
            return new List<IPAddress> { tuntapConfigTransfer.Info.IP }
            .Concat(tuntapConfigTransfer.Info.Lans.Where(c => c.Disabled == false).Select(c => c.IP)).ToList();
        }
    }
}
