using linker.libs.extends;
using linker.libs.websocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
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
                server.Start(port);
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            server.OnConnecting = (connection, header) =>
            {
                bool res = string.IsNullOrWhiteSpace(this.password) || (header.TryGetHeaderValue(WebsocketHeaderKey.SecWebSocketProtocol, out string _password) && string.Equals(_password, this.password));
                if (res)
                {
                    header.SetHeaderValue(WebsocketHeaderKey.SecWebSocketExtensions, string.Empty);
                }

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    header.TryGetHeaderValue(WebsocketHeaderKey.SecWebSocketProtocol, out string _password1);
                    LoggerHelper.Instance.Info($"websocket client password {_password1} eq {password}");
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
        public void SetPassword(string password)
        {
            this.password = password;
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
