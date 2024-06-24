using Linker.Libs.Api;
using System.Reflection;

namespace Linker.Client.Capi
{
    public interface IApiClientController : IApiController
    {

    }

    public interface IApiClientServer : IApiServer
    {
        public void LoadPlugins(Assembly[] assemblys);
    }

}
