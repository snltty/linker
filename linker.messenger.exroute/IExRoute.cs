using System.Net;

namespace linker.messenger.exroute
{
    public interface IExRoute
    {
        public List<IPAddress> Get();
    }
}
