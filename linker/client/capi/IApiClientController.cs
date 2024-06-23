using linker.libs.api;
using System.Reflection;

namespace linker.client.capi
{
    public interface IApiClientController : IApiController
    {

    }

    public interface IApiClientServer : IApiServer
    {
        public void LoadPlugins(Assembly[] assemblys);
    }

}
