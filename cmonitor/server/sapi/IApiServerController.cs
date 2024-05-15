using common.libs.api;
using System.Reflection;

namespace cmonitor.server.sapi
{
    public interface IApiServerController : IApiController
    {

    }

    public interface IApiServerServer : IApiServer
    {
        public void LoadPlugins(Assembly[] assemblys);
    }



}
