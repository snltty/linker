using common.libs.web;

namespace cmonitor.plugins.sapi
{
    public interface IWebServerServer : IWebServer
    {
    }

    /// <summary>
    /// 本地web管理端服务器
    /// </summary>
    public sealed class WebServerServer : WebServer, IWebServerServer
    {
    }

}
