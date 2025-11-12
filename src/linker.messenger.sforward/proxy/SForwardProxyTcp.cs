using linker.libs;
using linker.libs.extends;
using linker.messenger.sforward.server;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace linker.plugins.sforward.proxy
{
    public partial class SForwardProxy
    {
        private readonly ConcurrentDictionary<int, AsyncUserToken> tcpListens = new ConcurrentDictionary<int, AsyncUserToken>();
        private readonly ConcurrentDictionary<ulong, TaskCompletionSource<Socket>> tcpConnections = new ConcurrentDictionary<ulong, TaskCompletionSource<Socket>>();
        private readonly ConcurrentDictionary<ulong, AsyncUserToken> httpConnections = new ConcurrentDictionary<ulong, AsyncUserToken>();
        private readonly ConcurrentDictionary<string, SForwardTrafficCacheInfo> httpCaches = new ConcurrentDictionary<string, SForwardTrafficCacheInfo>();

        public Func<int, ulong, Task<string>> TunnelConnect { get; set; } = async (port, id) => { return await Task.FromResult(string.Empty).ConfigureAwait(false); };
        public Func<string, int, ulong, Task<string>> WebConnect { get; set; } = async (host, port, id) => { return await Task.FromResult(string.Empty).ConfigureAwait(false); };

        #region 服务端


        private void StartTcp(int port, bool isweb, byte bufferSize, string groupid, SForwardTrafficCacheInfo cache)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
            Socket socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.IPv6Only(localEndPoint.AddressFamily, false);
            socket.Bind(localEndPoint);
            socket.Listen(int.MaxValue);
            AsyncUserToken userToken = new AsyncUserToken
            {
                ListenPort = port,
                SourceSocket = socket,
                IsWeb = isweb,
                BufferSize = bufferSize,
                GroupId = groupid,
                Cache = cache
            };
            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs
            {
                UserToken = userToken,
                SocketFlags = SocketFlags.None,
            };

            acceptEventArg.Completed += IO_Completed;
            StartAccept(acceptEventArg);

            tcpListens.AddOrUpdate(port, userToken, (a, b) => userToken);
        }
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            acceptEventArg.AcceptSocket = null;
            AsyncUserToken token = (AsyncUserToken)acceptEventArg.UserToken;
            try
            {
                if (token.SourceSocket.AcceptAsync(acceptEventArg) == false)
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
                token.Clear();
            }
        }
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    ProcessAccept(e);
                    break;
                default:
                    break;
            }
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.AcceptSocket != null)
                {
                    AsyncUserToken acceptToken = (AsyncUserToken)e.UserToken;
                    Socket socket = e.AcceptSocket;
                    if (socket != null && socket.RemoteEndPoint != null)
                    {
                        socket.KeepAlive();
                        AsyncUserToken userToken = new AsyncUserToken
                        {
                            SourceSocket = socket,
                            ListenPort = acceptToken.ListenPort,
                            IsWeb = acceptToken.IsWeb,
                            BufferSize = acceptToken.BufferSize,
                            GroupId = acceptToken.GroupId,
                            Cache = acceptToken.Cache
                        };
                        _ = BindReceive(userToken);
                    }
                    StartAccept(e);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
        }
        private async Task BindReceive(AsyncUserToken token)
        {
            ulong id = ns.Increment();
            using IMemoryOwner<byte> buffer1 = MemoryPool<byte>.Shared.Rent((1 << token.BufferSize) * 1024);
            using IMemoryOwner<byte> buffer2 = MemoryPool<byte>.Shared.Rent((1 << token.BufferSize) * 1024);
            try
            {
                int length = await token.SourceSocket.ReceiveAsync(buffer1.Memory, SocketFlags.None).ConfigureAwait(false);
                //是回复连接。传过来了id，去配一下
                if (length > flagBytes.Length && buffer1.Memory.Span.Slice(0, flagBytes.Length).SequenceEqual(flagBytes))
                {
                    ulong _id = buffer1.Memory.Span.Slice(flagBytes.Length).ToUInt64();
                    if (tcpConnections.TryRemove(_id, out TaskCompletionSource<Socket> _tcs))
                    {
                        _tcs.TrySetResult(token.SourceSocket);
                    }
                    return;
                }

                string key = token.ListenPort.ToString();
                SForwardTrafficCacheInfo cache = token.Cache;
                //是web的，去获取host请求头，匹配不同的服务
                if (token.IsWeb)
                {

                    httpConnections.TryAdd(id, token);
                    key = token.Host = GetHost(buffer1.Memory.Slice(0, length));
                    if (string.IsNullOrWhiteSpace(token.Host))
                    {
                        if (Write404(token, buffer1.Memory,"Http host not found"))
                        {
                            return;
                        }
                        CloseClientSocket(token);
                        return;
                    }
                    string error = await WebConnect(token.Host, token.ListenPort, id).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(error) == false)
                    {
                        if (Write404(token, buffer1.Memory, error))
                        {
                            return;
                        }
                        CloseClientSocket(token);
                        return;
                    }
                    httpCaches.TryGetValue(token.Host, out cache);
                }
                else
                {
                    //纯TCP的，直接拿端口去匹配
                    string error = await TunnelConnect(token.ListenPort, id).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(error) == false)
                    {
                        if (Write404(token, buffer1.Memory, error))
                        {
                            return;
                        }
                        CloseClientSocket(token);
                        return;
                    }
                }

                //等待回复
                TaskCompletionSource<Socket> tcs = new TaskCompletionSource<Socket>(TaskCreationOptions.RunContinuationsAsynchronously);
                tcpConnections.TryAdd(id, tcs);
                token.TargetSocket = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);

                Add(key, token.GroupId, length, length);
                await token.TargetSocket.SendAsync(buffer1.Memory.Slice(0, length)).ConfigureAwait(false);

                //两端交换数据
                await Task.WhenAll(CopyToAsync(key, token.GroupId, buffer1.Memory, token.SourceSocket, token.TargetSocket, cache), CopyToAsync(key, token.GroupId, buffer2.Memory, token.TargetSocket, token.SourceSocket, cache)).ConfigureAwait(false);

                CloseClientSocket(token);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                CloseClientSocket(token);
            }
            finally
            {
                tcpConnections.TryRemove(id, out _);
                httpConnections.TryRemove(id, out _);
            }
        }

        private void CloseClientSocket(AsyncUserToken token)
        {
            if (token == null) return;
            token.Clear();
        }
        public void StopTcp(int port)
        {
            if (tcpListens.TryRemove(port, out AsyncUserToken userToken))
            {
                if (userToken.Cache != null)
                {
                    sForwardServerNodeTransfer.RemoveTrafficCache(userToken.Cache.Cache.FlowId);
                }
                CloseClientSocket(userToken);
            }
        }

        private bool Write404(AsyncUserToken token, Memory<byte> buffer,string error)
        {
            try
            {
                if (buffer.Slice(0, getBytes.Length).Span.SequenceEqual(getBytes) == false)
                {
                    return false;
                }

                string path404 = Path.Join(Helper.CurrentDirectory, "web", "404.html");
                string path404Default = Path.Join(Helper.CurrentDirectory, "web", "404_default.html");

                string f0f = File.Exists(path404) ? File.ReadAllText(path404) : File.Exists(path404Default) ? File.ReadAllText(path404Default) : string.Empty;
                if (string.IsNullOrWhiteSpace(f0f))
                {
                    return false;
                }
                f0f = f0f.Replace("{{error}}", error);
                string response = $"HTTP/1.1 200 OK\r\nContent-Length: {f0f.Length}\r\nContent-type: text/html\r\nServer: linker\r\nConnection: close\r\n\r\n{f0f}";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                token.SourceSocket.Send(responseBytes);

                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }
        public void AddHttp(string host, bool super, double bandwidth)
        {
            if (host.Contains('.') == false)
            {
                host = $"{host}.{sForwardServerNodeTransfer.Node.Domain}";
            }

            SForwardTrafficCacheInfo sForwardTrafficCacheInfo = sForwardServerNodeTransfer.AddTrafficCache(super, bandwidth);
            httpCaches.AddOrUpdate(host, sForwardTrafficCacheInfo, (a, b) => sForwardTrafficCacheInfo);
        }
        public void RemoveHttp(string host)
        {
            if (host.Contains('.') == false)
            {
                host = $"{host}.{sForwardServerNodeTransfer.Node.Domain}";
            }
            if (httpCaches.TryRemove(host, out var cache))
            {
                sForwardServerNodeTransfer.RemoveTrafficCache(cache.Cache.FlowId);
            }
            foreach (var item in httpConnections.Where(c => c.Value.Host == host).Select(c => c.Key).ToList())
            {
                if (httpConnections.TryRemove(item, out var token))
                {
                    CloseClientSocket(token);
                }
            }
        }

        private readonly byte[] getBytes = Encoding.UTF8.GetBytes("GET / HTTP/");
        private readonly byte[] hostBytes = Encoding.UTF8.GetBytes("Host: ");
        private readonly byte[] wrapBytes = Encoding.UTF8.GetBytes("\r\n");
        private readonly byte[] colonBytes = Encoding.UTF8.GetBytes(":");
        private string GetHost(Memory<byte> buffer)
        {
            int start = buffer.Span.IndexOf(hostBytes);
            if (start < 0) return string.Empty;
            start += hostBytes.Length;

            int length = buffer.Span.Slice(start).IndexOf(wrapBytes);

            int length1 = buffer.Span.Slice(start, length).IndexOf(colonBytes);
            if (length1 > 0) length = length1;

            return Encoding.UTF8.GetString(buffer.Slice(start, length).Span);
        }


        #endregion

        /// <summary>
        /// 客户端，收到服务端的连接请求
        /// </summary>
        /// <param name="key"></param>
        /// <param name="bufferSize"></param>
        /// <param name="id"></param>
        /// <param name="server"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public async Task OnConnectTcp(string key, byte bufferSize, ulong id, IPEndPoint server, IPEndPoint service)
        {
            Socket sourceSocket = null;
            Socket targetSocket = null;
            using IMemoryOwner<byte> buffer1 = MemoryPool<byte>.Shared.Rent((1 << bufferSize) * 1024);
            using IMemoryOwner<byte> buffer2 = MemoryPool<byte>.Shared.Rent((1 << bufferSize) * 1024);
            try
            {
                //连接服务器
                sourceSocket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await sourceSocket.ConnectAsync(server).ConfigureAwait(false);

                //连接本地服务
                targetSocket = new Socket(service.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await targetSocket.ConnectAsync(service).ConfigureAwait(false);

                //给服务器回复，带上id
                flagBytes.AsMemory().CopyTo(buffer1.Memory);
                id.ToBytes(buffer1.Memory.Slice(flagBytes.Length));
                await sourceSocket.SendAsync(buffer1.Memory.Slice(0, flagBytes.Length + 8)).ConfigureAwait(false);

                //交换数据即可
                await Task.WhenAll(CopyToAsync($"{key}->{service}", string.Empty, buffer1.Memory, sourceSocket, targetSocket), CopyToAsync($"{key}->{service}", string.Empty, buffer2.Memory, targetSocket, sourceSocket)).ConfigureAwait(false);

            }
            catch (Exception)
            {
                sourceSocket?.SafeClose();
                targetSocket?.SafeClose();
            }
            finally
            {
            }
        }

        /// <summary>
        /// 读取数据，然后发送给对方，用户两端交换数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="groupid"></param>
        /// <param name="buffer"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private async Task CopyToAsync(string key, string groupid, Memory<byte> buffer, Socket source, Socket target, SForwardTrafficCacheInfo trafficCacheInfo = null)
        {
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReceiveAsync(buffer, SocketFlags.None).ConfigureAwait(false)) != 0)
                {
                    if (trafficCacheInfo != null)
                    {
                        //流量限制
                        if (sForwardServerNodeTransfer.AddBytes(trafficCacheInfo, bytesRead) == false)
                        {
                            source.SafeClose();
                            break;
                        }

                        //总速度
                        if (sForwardServerNodeTransfer.NeedLimit(trafficCacheInfo))
                        {
                            int length = bytesRead;
                            sForwardServerNodeTransfer.TryLimit(ref length);
                            while (length > 0)
                            {
                                await Task.Delay(30).ConfigureAwait(false);
                                sForwardServerNodeTransfer.TryLimit(ref length);
                            }
                        }
                        //单个速度
                        if (trafficCacheInfo.Limit.NeedLimit())
                        {
                            int length = bytesRead;
                            trafficCacheInfo.Limit.TryLimit(ref length);
                            while (length > 0)
                            {
                                await Task.Delay(30).ConfigureAwait(false);
                                trafficCacheInfo.Limit.TryLimit(ref length);
                            }
                        }
                    }
                    Add(key, groupid, bytesRead, bytesRead);
                    await target.SendAsync(buffer.Slice(0, bytesRead), SocketFlags.None).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            finally
            {
                source.SafeClose();
                target.SafeClose();
            }
        }

    }

    public sealed class AsyncUserToken
    {
        public int ListenPort { get; set; }
        public string Host { get; set; }
        public string GroupId { get; set; }
        public bool IsWeb { get; set; }
        public Socket SourceSocket { get; set; }
        public Socket TargetSocket { get; set; }

        public byte BufferSize { get; set; }

        public SForwardTrafficCacheInfo Cache { get; set; }

        public void Clear()
        {
            SourceSocket?.SafeClose();
            TargetSocket?.SafeClose();

            GC.Collect();
        }
    }
}
