using System.Net;

namespace linker.plugins.route
{
    public sealed partial class RouteExcludeIPTransfer
    {
        private List<IRouteExcludeIP> excludeIPs;

        public RouteExcludeIPTransfer()
        {
        }

        public void LoadTunnelExcludeIPs(List<IRouteExcludeIP> list)
        {
            excludeIPs = list;
           
        }

        public List<IPAddress> Get()
        {
            List<IPAddress> result = new List<IPAddress>();
            foreach (var item in excludeIPs)
            {
                result.AddRange(item.Get());
            }
            return result;
        }
    }
}
