using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace linker.libs.web
{
    /// <summary>
    /// 本地web管理端服务器
    /// </summary>
    public class WebServer : IWebServer
    {
        private string root = "";

        private readonly IWebServerFileReader fileReader;
        public WebServer(IWebServerFileReader fileReader)
        {
            this.fileReader = fileReader;
        }

        /// <summary>
        /// 开启web
        /// </summary>
        public void Start(int port, string root)
        {
            this.root = root;
            Task.Factory.StartNew(() =>
            {
                try
                {
                    HttpListener http = new HttpListener();
                    http.IgnoreWriteExceptions = true;
                    http.Prefixes.Add($"http://+:{port}/");
                    http.Start();

                    http.BeginGetContext(Callback, http);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Error(ex);
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
                response.Headers.Set("Server", Helper.GlobalString);

                string path = request.Url.AbsolutePath;
                //默认页面
                if (path == "/") path = "index.html";

                Memory<byte> memory = Helper.EmptyArray;
                DateTime last = DateTime.Now;
                try
                {
                    memory = fileReader.Read(root, path, out last);
                    if (memory.Length > 0)
                    {
                        response.ContentLength64 = memory.Length;
                        response.ContentType = GetContentType(path);
                        if (OperatingSystem.IsAndroid())
                        {
                            response.Headers.Set("Last-Modified", last.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                        else
                        {
                            response.Headers.Set("Last-Modified", last.ToString());
                        }
                        response.OutputStream.Write(memory.Span);
                        response.OutputStream.Flush();
                        response.OutputStream.Close();
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
            { ".zip","application/zip"},
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


    public interface IWebServerFileReader
    {
        public byte[] Read(string root, string fileName, out DateTime lastModified);
    }
    public sealed class WebServerFileReader : IWebServerFileReader
    {
        public byte[] Read(string root, string fileName, out DateTime lastModified)
        {
            fileName = Path.Join(root, fileName);
            lastModified = File.GetLastWriteTimeUtc(fileName);
            return File.ReadAllBytes(fileName);
        }
    }
}
