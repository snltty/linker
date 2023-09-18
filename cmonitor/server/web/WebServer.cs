using common.libs;
using System.Net;

namespace cmonitor.server.web
{
    /// <summary>
    /// 本地web管理端服务器
    /// </summary>
    public sealed class WebServer : IWebServer
    {
        private readonly Config config;
        public WebServer(Config config)
        {
            this.config = config;
        }

        /// <summary>
        /// 开启web
        /// </summary>
        public void Start()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    HttpListener http = new HttpListener();
                    http.IgnoreWriteExceptions = true;
                    http.Prefixes.Add($"http://+:{config.WebPort}/");
                    http.Start();

                    http.BeginGetContext(Callback, http);
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }, TaskCreationOptions.LongRunning);
        }
        private void Callback(IAsyncResult result)
        {
            HttpListener http = result.AsyncState as HttpListener;
            HttpListenerContext context = http.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                response.Headers.Set("Server", "snltty");

                string path = request.Url.AbsolutePath;
                //默认页面
                if (path == "/") path = "index.html";


                path = Path.Join(config.WebRoot, path);
                if (File.Exists(path))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    response.ContentLength64 = bytes.Length;
                    response.ContentType = GetContentType(path);
                    response.Headers.Set("Last-Modified", File.GetLastWriteTimeUtc(path).ToString());

                    response.OutputStream.Write(bytes, 0, bytes.Length);
                    response.OutputStream.Flush();
                    response.OutputStream.Close();
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            catch (Exception)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
            }

            response.Close();

            http.BeginGetContext(Callback, http);
        }


        private Dictionary<string, string> types = new Dictionary<string, string> {
            { ".webp","image/webp"},
            { ".png","image/png"},
            { ".jpg","image/jpg"},
            { ".jpeg","image/jpeg"},
            { ".gif","image/gif"},
            { ".svg","image/svg+xml"},
            { ".ico","image/x-icon"},
            { ".js","text/javascript; charset=utf-8"},
            { ".html","text/html; charset=utf-8"},
            { ".css","text/css; charset=utf-8"},
            { ".pac","application/x-ns-proxy-autoconfig; charset=utf-8"},
        };
        private string GetContentType(string path)
        {
            string ext = Path.GetExtension(path);
            if (types.ContainsKey(ext))
            {
                return types[ext];
            }
            return "application/octet-stream";
        }
    }

}
