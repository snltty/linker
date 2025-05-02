using linker.libs;
using System.Net;

namespace linker.messenger.exroute
{
    public sealed partial class ExRouteTransfer
    {
        private List<IExRoute> excludes = new List<IExRoute>();

        public ExRouteTransfer()
        {
        }

        public void AddExRoutes(List<IExRoute> list)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"add exroute {string.Join(",", list.Select(c => c.GetType().Name))}");
            excludes = excludes.Concat(list).Distinct().ToList();
           
        }

        public List<IPAddress> Get()
        {
            List<IPAddress> result = new List<IPAddress>();
            foreach (var item in excludes)
            {
                result.AddRange(item.Get());
            }
            return result;
        }
    }
}
