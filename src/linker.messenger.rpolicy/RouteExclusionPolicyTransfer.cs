using linker.libs;
using System.Net;

namespace linker.messenger.rpolicy
{
    public sealed partial class RouteExclusionPolicyTransfer
    {
        private List<IRouteExclusionPolicy> policys = new List<IRouteExclusionPolicy>();

        public RouteExclusionPolicyTransfer()
        {
        }

        public void AddRouteExclusionPolicys(List<IRouteExclusionPolicy> list)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"add route exclusion policys {string.Join(",", list.Select(c => c.GetType().Name))}");
            policys = policys.Concat(list).Distinct().ToList();

        }

        public List<IPAddress> Query()
        {
            List<IPAddress> result = new List<IPAddress>();
            foreach (var item in policys)
            {
                result.AddRange(item.Query());
            }
            return result;
        }
    }
}
