using linker.libs.api;

namespace linker.messenger.api
{
    public interface IApiServer : libs.api.IApiServer
    {
        public void AddPlugins(List<IApiController> list);
    }

}
