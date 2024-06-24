using Linker.Libs.Web;

namespace Linker.Plugins.Capi
{
    public interface IWebClientServer : IWebServer
    {
    }

    /// <summary>
    /// 本地web管理端服务器
    /// </summary>
    public sealed class WebClientServer : WebServer, IWebClientServer
    {
    }

}
