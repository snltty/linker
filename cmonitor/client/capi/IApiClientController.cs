using common.libs.api;
using System.Reflection;

namespace cmonitor.client.capi
{
    public interface IApiClientController : IApiController
    {

    }

    public interface IApiClientServer : IApiServer
    {
        public void LoadPlugins(Assembly[] assemblys);
    }

}
