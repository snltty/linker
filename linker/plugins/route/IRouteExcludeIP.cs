using System.Net;

namespace linker.plugins.route
{
    public interface IRouteExcludeIP
    {
        public List<IPAddress> Get();
    }
}
