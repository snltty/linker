using System.Net;

namespace linker.messenger.rpolicy
{
    public interface IRouteExclusionPolicy
    {
        public List<IPAddress> Query();
    }
}
