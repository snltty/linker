using link.libs.api;
using System.Reflection;

namespace link.client.capi
{
    public interface IApiClientController : IApiController
    {

    }

    public interface IApiClientServer : IApiServer
    {
        public void LoadPlugins(Assembly[] assemblys);
    }

}
