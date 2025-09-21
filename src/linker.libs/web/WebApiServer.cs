using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace linker.libs.web
{
    /// <summary>
    /// 本地web api 服务器
    /// </summary>
    public class WebApiServer : IWebApiServer
    {
        private readonly ConcurrentDictionary<string, IWebApiController> dic = new();

        public WebApiServer()
        {
        }

        /// <summary>
        /// 开启web api
        /// </summary>
        public void Start(int port)
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    HttpListener http = new HttpListener();
                    http.IgnoreWriteExceptions = true;
                    http.Prefixes.Add($"http://+:{port}/");
                    http.Start();

                    while (true)
                    {
                        try
                        {
                            HttpListenerContext context = await http.GetContextAsync();
                            HandleWeb(context);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public void AddController(IWebApiController controller)
        {
            if (controller == null || string.IsNullOrWhiteSpace(controller.Path)) return;

            dic.TryAdd(controller.Path.ToLower(), controller);
        }
        public void AddControllers(List<IWebApiController> controllers)
        {
            foreach (var controller in controllers)
            {
                if (controller == null || string.IsNullOrWhiteSpace(controller.Path)) continue;

                dic.TryAdd(controller.Path.ToLower(), controller);
            }
        }
        private void HandleWeb(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            try
            {
                response.Headers.Set("Server", Helper.GlobalString);

                string path = request.Url.AbsolutePath.ToLower();
                string query = request.Url.Query;
                //默认页面
                if (path == "/") path = "online.json";

                try
                {
                    if (dic.TryGetValue(path, out IWebApiController controller))
                    {
                        Memory<byte> memory = controller.Handle(query);
                        response.ContentLength64 = memory.Length;
                        response.ContentType = "application/json";
                        response.OutputStream.Write(memory.Span);
                        response.OutputStream.Flush();
                        response.OutputStream.Close();

                        controller.Free();
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                }
                catch (Exception ex)
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(ex + $"");
                    response.ContentLength64 = bytes.Length;
                    response.ContentType = "text/plain; charset=utf-8";
                    response.OutputStream.Write(bytes, 0, bytes.Length);
                    response.OutputStream.Flush();
                    response.OutputStream.Close();
                }
            }
            catch (Exception)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
            }

            response.Close();
        }

    }

}
