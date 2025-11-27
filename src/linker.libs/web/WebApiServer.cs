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
                        HttpListenerContext context = await http.GetContextAsync();
                        _ = HandleWeb(context);
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
        private async Task HandleWeb(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            try
            {
                response.Headers.Set("Server", Helper.GlobalString);
                response.Headers.Set("Access-Control-Allow-Origin", "*");
                response.Headers.Set("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                response.Headers.Set("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
                response.Headers.Set("Access-Control-Allow-Credentials", "true");
                response.Headers.Set("Access-Control-Max-Age", "86400");

                if (context.Request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.Close();
                    return;
                }

                try
                {
                    string path = request.Url.AbsolutePath.ToLower();
                    string query = request.Url.Query;
                    //默认页面
                    if (path == "/") path = "online.json";

                    if (dic.TryGetValue(path, out IWebApiController controller))
                    {
                        Memory<byte> memory = await controller.Handle(query);
                        response.ContentLength64 = memory.Length;
                        response.ContentType = "application/json";
                        await response.OutputStream.WriteAsync(memory);
                        await response.OutputStream.FlushAsync();
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
                    await response.OutputStream.WriteAsync(bytes.AsMemory(0, bytes.Length));
                    await response.OutputStream.FlushAsync();
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
