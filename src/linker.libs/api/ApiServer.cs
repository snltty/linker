using linker.libs.extends;
using linker.libs.websocket;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace linker.libs.api
{
    /// <summary>
    /// 前段接口服务
    /// </summary>
    public class ApiServer : IApiServer
    {
        protected readonly Dictionary<string, PluginPathCacheInfo> plugins = new();
        protected readonly ConcurrentDictionary<uint, ConnectionTimeInfo> connectionTimes = new();
        public uint OnlineNum = 0;
        private string password = string.Empty;

        private WebSocketServer server;

        public ApiServer()
        {
        }

        /// <summary>
        /// 开启websockt
        /// </summary>
        public void Websocket(int port, string password = "")
        {
            this.password = password;
            server = new WebSocketServer();
            try
            {
                server.Start( port);
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            server.OnConnecting = (connection, header) =>
            {
                bool res = string.IsNullOrWhiteSpace(this.password) || (header.TryGetHeaderValue(WebsocketHeaderKey.SecWebSocketProtocol, out string _password) && _password.Contains(this.password));
                if (res)
                {
                    header.SetHeaderValue(WebsocketHeaderKey.SecWebSocketExtensions, string.Empty);
                }
                return res;
            };
            server.OnOpen = (connection) =>
            {
                Interlocked.Increment(ref OnlineNum);
                connectionTimes.TryAdd(connection.Id, new ConnectionTimeInfo());
            };
            server.OnDisConnectd = (connection) =>
            {
                Interlocked.Decrement(ref OnlineNum);
                if (OnlineNum < 0) Interlocked.Exchange(ref OnlineNum, 0);
                connectionTimes.TryRemove(connection.Id, out _);
            };
            server.OnMessage = (connection, frame, message) =>
            {
                if (connectionTimes.TryGetValue(connection.Id, out ConnectionTimeInfo timeInfo))
                {
                    timeInfo.DateTime = DateTime.Now;
                }
                var req = message.DeJson<ApiControllerRequestInfo>();
                req.Connection = connection;
                OnMessage(req).ContinueWith((result) =>
                {
                    var resp = result.Result.ToJson().ToBytes();
                    connection.SendFrameText(resp);
                });
            };
        }

        /// <summary>
        /// 收到消息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<ApiControllerResponseInfo> OnMessage(ApiControllerRequestInfo model)
        {
            model.Path = model.Path.ToLower();
            if (plugins.TryGetValue(model.Path, out PluginPathCacheInfo plugin) == false)
            {
                return new ApiControllerResponseInfo
                {
                    Content = $"{model.Path} not exists",
                    RequestId = model.RequestId,
                    Path = model.Path,
                    Code = ApiControllerResponseCodes.NotFound
                };
            }
            if (plugin.HasAccess(plugin.Access) == false)
            {
                return new ApiControllerResponseInfo
                {
                    Content = "no permission",
                    RequestId = model.RequestId,
                    Path = model.Path,
                    Code = ApiControllerResponseCodes.Error
                };
            }

            try
            {
                ApiControllerParamsInfo param = new ApiControllerParamsInfo
                {
                    RequestId = model.RequestId,
                    Content = model.Content,
                    Connection = model.Connection
                };
                dynamic resultAsync = plugin.Method.Invoke(plugin.Target, new object[] { param });
                object resultObject = null;
                if (plugin.IsVoid == false)
                {
                    if (plugin.IsTask)
                    {
                        await resultAsync.ConfigureAwait(false);
                        if (plugin.IsTaskResult)
                        {
                            resultObject = resultAsync.Result;
                        }
                    }
                    else
                    {
                        resultObject = resultAsync;
                    }
                }
                return new ApiControllerResponseInfo
                {
                    Code = param.Code,
                    Content = param.Code != ApiControllerResponseCodes.Error ? resultObject : param.ErrorMessage,
                    RequestId = model.RequestId,
                    Path = model.Path,
                };
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error($"{model.Path} -> {ex.Message}");
                return new ApiControllerResponseInfo
                {
                    Content = ex.Message,
                    RequestId = model.RequestId,
                    Path = model.Path,
                    Code = ApiControllerResponseCodes.Error
                };
            }
        }

        public void Notify(string path, object content)
        {
            if (server.Connections.Any())
            {
                try
                {
                    byte[] bytes = JsonSerializer.Serialize(new ApiControllerResponseInfo
                    {
                        Code = ApiControllerResponseCodes.Success,
                        Content = content,
                        Path = path,
                        RequestId = 0
                    }).ToBytes();

                    foreach (WebsocketConnection connection in server.Connections)
                    {
                        if (connection.Connected && connectionTimes.TryGetValue(connection.Id, out ConnectionTimeInfo timeInfo) && (DateTime.Now - timeInfo.DateTime).TotalMilliseconds < 1000)
                        {
                            try
                            {
                                connection.SendFrameText(bytes);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
        }

        public void Notify(string path, string name, ReadOnlyMemory<byte> content)
        {
            if (server.Connections.Any())
            {
                try
                {
                    Memory<byte> headMemory = JsonSerializer.Serialize(new ApiControllerResponseInfo
                    {
                        Code = ApiControllerResponseCodes.Success,
                        Content = name,
                        Path = path,
                        RequestId = 0
                    }).ToBytes();

                    int length = 4 + headMemory.Length + content.Length;
                    byte[] result = ArrayPool<byte>.Shared.Rent(length);

                    int index = 0;
                    headMemory.Length.ToBytes(result);
                    index += 4;
                    headMemory.CopyTo(result.AsMemory(index));
                    index += headMemory.Length;
                    content.CopyTo(result.AsMemory(index));
                    index += content.Length;

                    foreach (WebsocketConnection connection in server.Connections)
                    {
                        if (connection.Connected && connectionTimes.TryGetValue(connection.Id, out ConnectionTimeInfo timeInfo) && (DateTime.Now - timeInfo.DateTime).TotalMilliseconds < 1000)
                        {
                            try
                            {
                                connection.SendFrameBinary(result.AsMemory(0, length));
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    ArrayPool<byte>.Shared.Return(result);
                }
                catch (Exception)
                {
                    //LoggerHelper.Instance.Error(ex);
                }
            }
        }

        public void Notify(string path, object content, WebsocketConnection connection)
        {
            try
            {
                if (connection.Connected == false) return;

                byte[] bytes = JsonSerializer.Serialize(new ApiControllerResponseInfo
                {
                    Code = ApiControllerResponseCodes.Success,
                    Content = content,
                    Path = path,
                    RequestId = 0
                }).ToBytes();

                try
                {
                    connection.SendFrameText(bytes);
                }
                catch (Exception)
                {
                }
            }
            catch (Exception)
            {
                //LoggerHelper.Instance.Error(ex);
            }
        }
    }

    public sealed class ConnectionTimeInfo
    {
        public DateTime DateTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 前段接口缓存
    /// </summary>
    public struct PluginPathCacheInfo
    {
        /// <summary>
        /// 对象
        /// </summary>
        public object Target { get; set; }
        /// <summary>
        /// 方法
        /// </summary>
        public MethodInfo Method { get; set; }
        /// <summary>
        /// 是否void
        /// </summary>
        public bool IsVoid { get; set; }
        /// <summary>
        /// 是否task
        /// </summary>
        public bool IsTask { get; set; }
        /// <summary>
        /// 是否task result
        /// </summary>
        public bool IsTaskResult { get; set; }

        public ulong Access { get; set; }
        public Func<ulong, bool> HasAccess { get; set; }
    }

}
