using linker.libs.api;

namespace linker.plugins.capi
{
    public interface IApiClientController : IApiController
    {

    }

    public interface IApiClientServer : IApiServer
    {
        public void LoadPlugins(List<object> list);
    }

}
