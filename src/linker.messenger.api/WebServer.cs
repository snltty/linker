using linker.libs.web;

namespace linker.messenger.api
{
    /// <summary>
    /// 本地web管理端服务器
    /// </summary>
    public sealed class WebServer : libs.web.WebServer, IWebServer
    {
        public WebServer(IWebServerFileReader webServerFileReader) : base(webServerFileReader)
        {

        }
    }

}
