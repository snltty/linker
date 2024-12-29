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
