using cmonitor.tunnel.connection;
using common.libs;
using common.libs.extends;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.tunnel.proxy
{
    public partial class TunnelProxy
    {
        private ConcurrentDictionary<int, AsyncUserToken> tcpListens = new ConcurrentDictionary<int, AsyncUserToken>();
        private readonly ConcurrentDictionary<ConnectId, AsyncUserToken> tcpConnections = new ConcurrentDictionary<ConnectId, AsyncUserToken>(new ConnectIdComparer());
        private Socket socket;
        public IPEndPoint LocalEndpoint => socket?.LocalEndPoint as IPEndPoint ?? new IPEndPoint(IPAddress.Any, 0);

        /// <summary>
        /// 监听一个端口
        /// </summary>
        /// <param name="port"></param>
        private void StartTcp(IPEndPoint ep)
        {
            try
            {
                IPEndPoint localEndPoint = ep;
                socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.IPv6Only(localEndPoint.AddressFamily, false);
                socket.ReuseBind(localEndPoint);
                socket.Listen(int.MaxValue);
                AsyncUserToken userToken = new AsyncUserToken
                {
                    ListenPort = localEndPoint.Port,
                    Socket = socket
                };
                SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs
                {
                    UserToken = userToken,
                    SocketFlags = SocketFlags.None,
                };
                userToken.Saea = acceptEventArg;

                acceptEventArg.Completed += IO_Completed;
                StartAccept(acceptEventArg);

                tcpListens.AddOrUpdate(localEndPoint.Port, userToken, (a, b) => userToken);


            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        /// <summary>
        /// 接收连接
        /// </summary>
        /// <param name="acceptEventArg"></param>
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            acceptEventArg.AcceptSocket = null;
            AsyncUserToken token = (AsyncUserToken)acceptEventArg.UserToken;
            try
            {
                if (token.Socket.AcceptAsync(acceptEventArg) == false)
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
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
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
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
                            Socket = socket,
                            Received = false,
                            Paused = false,
                            ListenPort = acceptToken.ListenPort,
                            Proxy = new ProxyInfo { Data = Helper.EmptyArray, Step = ProxyStep.Request, Port = (ushort)acceptToken.ListenPort, ConnectId = ns.Increment() }
                        };
                        BindReceive(userToken);
                    }
                    StartAccept(e);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        /// <summary>
        /// 接收连接数据
        /// </summary>
        /// <param name="token"></param>
        private void BindReceive(AsyncUserToken token)
        {
            try
            {
                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = token,
                    SocketFlags = SocketFlags.None,
                };
                token.Saea = readEventArgs;

                readEventArgs.SetBuffer(new byte[8 * 1024], 0, 8 * 1024);
                readEventArgs.Completed += IO_Completed;
                if (token.Socket.ReceiveAsync(readEventArgs) == false)
                {
                    ProcessReceive(readEventArgs);
                }

            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }
        }
        /// <summary>
        /// 接收连接数据
        /// </summary>
        /// <param name="e"></param>
        private async void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int offset = e.Offset;
                    int length = e.BytesTransferred;
                    await ReadPacket(token, e.Buffer.AsMemory(offset, length)).ConfigureAwait(false);

                    if (token.Received == false)
                    {
                        token.Paused = true;
                        return;
                    }
                    token.Paused = false;

                    if (token.Socket.Available > 0)
                    {
                        while (token.Socket.Available > 0)
                        {
                            length = token.Socket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                await ReadPacket(token, e.Buffer.AsMemory(0, length));
                            }
                            else
                            {
                                await SendToConnectionClose(token).ConfigureAwait(false);
                                CloseClientSocket(token);
                                return;
                            }
                            if (token.Received == false)
                            {
                                token.Paused = true;
                                return;
                            }
                            token.Paused = false;
                        }
                    }

                    if (token.Socket.Connected == false)
                    {
                        await SendToConnectionClose(token).ConfigureAwait(false);
                        CloseClientSocket(token);
                        return;
                    }

                    if (token.Socket.ReceiveAsync(e) == false)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        Logger.Instance.Error(e.SocketError.ToString());

                    await SendToConnectionClose(token).ConfigureAwait(false);
                    CloseClientSocket(token);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
                await SendToConnectionClose(token).ConfigureAwait(false);
                CloseClientSocket(token);
            }
        }
        /// <summary>
        /// 处理连接数据，b端收到数据，发给a，a端收到数据，发给b，通过隧道
        /// </summary>
        /// <param name="token"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private async Task ReadPacket(AsyncUserToken token, Memory<byte> data)
        {
            token.Proxy.Data = data;
            if (token.Proxy.Step == ProxyStep.Request)
            {
                bool closeConnect = await ConnectTunnelConnection(token);
                if (token.Connection != null)
                {
                    if (token.Proxy.TargetEP != null)
                    {
                        await SendToConnection(token).ConfigureAwait(false);
                    }
                    token.Proxy.Step = ProxyStep.Forward;

                    //绑定
                    tcpConnections.TryAdd(new ConnectId(token.Proxy.ConnectId, token.Connection.GetHashCode(), (byte)ProxyDirection.Reverse), token);
                }
                else if (closeConnect)
                {
                    CloseClientSocket(token);
                }
            }
            else
            {
                await SendToConnection(token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 连接到TCP转发
        /// </summary>
        /// <param name="token"></param>
        /// <returns>当未获得通道连接对象时，是否关闭连接</returns>
        protected virtual async ValueTask<bool> ConnectTunnelConnection(AsyncUserToken token)
        {
            return await ValueTask.FromResult(false);
        }
        /// <summary>
        /// 往隧道发数据
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task SendToConnection(AsyncUserToken token)
        {
            SemaphoreSlim semaphoreSlim = token.Proxy.Direction == ProxyDirection.Forward ? semaphoreSlimForward : semaphoreSlimReverse;
            await semaphoreSlim.WaitAsync();

            byte[] connectData = token.Proxy.ToBytes(out int length);

            try
            {
                bool res = await token.Connection.SendAsync(connectData.AsMemory(0, length)).ConfigureAwait(false);
                if (res == false)
                {
                    CloseClientSocket(token);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
                CloseClientSocket(token);
            }
            finally
            {
                token.Proxy.Return(connectData);
                semaphoreSlim.Release();
            }
        }
        /// <summary>
        /// 往隧道发关闭包数据，通知对面关闭连接
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task SendToConnectionClose(AsyncUserToken token)
        {
            //if (token.Proxy.Direction == ProxyDirection.Reverse)
            {
                ProxyStep step = token.Proxy.Step;
                token.Proxy.Step = ProxyStep.Close;
                token.Proxy.Data = Helper.EmptyArray;
                await SendToConnection(token);
                token.Proxy.Step = step;
            }
        }

        /// <summary>
        /// b端连接目标服务
        /// </summary>
        /// <param name="token"></param>
        private void ConnectBind(AsyncUserTunnelToken token)
        {
            if (token.Proxy.TargetEP == null) return;

            Socket socket = new Socket(token.Proxy.TargetEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.KeepAlive();

            ConnectState state = new ConnectState { Connection = token.Connection, ConnectId = token.Proxy.ConnectId, Socket = socket, IPEndPoint = token.Proxy.TargetEP };
            state.CopyData(token.Proxy.Data);
            socket.BeginConnect(token.Proxy.TargetEP, ConnectCallback, state);

        }
        private async void ConnectCallback(IAsyncResult result)
        {
            ConnectState state = result.AsyncState as ConnectState;
            AsyncUserToken token = new AsyncUserToken
            {
                Connection = state.Connection,
                Socket = state.Socket,
                Received = true,
                Paused = false,
                Proxy = new ProxyInfo
                {
                    ConnectId = state.ConnectId,
                    Step = ProxyStep.Receive,
                    Direction = ProxyDirection.Reverse,
                    Protocol = ProxyProtocol.Tcp
                }
            };
            try
            {
                token.Socket.EndConnect(result);
                token.Socket.KeepAlive();

                if (state.Data.Length > 0)
                {
                    await token.Socket.SendAsync(state.Data.AsMemory(0, state.Length), SocketFlags.None);
                }
                tcpConnections.TryAdd(new ConnectId(token.Proxy.ConnectId, token.Connection.GetHashCode(), (byte)ProxyDirection.Forward), token);

                await SendToConnection(token).ConfigureAwait(false);
                token.Proxy.Step = ProxyStep.Forward;

                BindReceive(token);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"connect {state.IPEndPoint} error -> {ex}");
                await SendToConnectionClose(token).ConfigureAwait(false);
                CloseClientSocket(token);
            }
            finally
            {
                state.ClearData();
            }
        }

        /// <summary>
        /// 暂停接收数据
        /// </summary>
        /// <param name="tunnelToken"></param>
        private void PauseSocket(AsyncUserTunnelToken tunnelToken)
        {
            if (tunnelToken.Proxy.Protocol == ProxyProtocol.Tcp)
            {
                ConnectId connectId = new ConnectId(tunnelToken.Proxy.ConnectId, tunnelToken.Connection.GetHashCode(), (byte)tunnelToken.Proxy.Direction);
                if (tcpConnections.TryGetValue(connectId, out AsyncUserToken token))
                {
                    token.Received = false;
                }
            }
        }
        /// <summary>
        /// 继续接收数据
        /// </summary>
        /// <param name="tunnelToken"></param>
        private void ReceiveSocket(AsyncUserTunnelToken tunnelToken)
        {
            if (tunnelToken.Proxy.Protocol == ProxyProtocol.Tcp)
            {
                ConnectId connectId = new ConnectId(tunnelToken.Proxy.ConnectId, tunnelToken.Connection.GetHashCode(), (byte)tunnelToken.Proxy.Direction);
                if (tcpConnections.TryGetValue(connectId, out AsyncUserToken token))
                {
                    if (token.Received == false)
                    {
                        token.Received = true;
                        if (token.Paused)
                        {
                            token.Paused = false;
                            if (token.Socket.ReceiveAsync(token.Saea) == false)
                            {
                                ProcessReceive(token.Saea);
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <param name="tunnelToken"></param>
        private void CloseSocket(AsyncUserTunnelToken tunnelToken)
        {
            if (tunnelToken.Proxy.Protocol == ProxyProtocol.Tcp)
            {
                ConnectId connectId = new ConnectId(tunnelToken.Proxy.ConnectId, tunnelToken.Connection.GetHashCode(), (byte)tunnelToken.Proxy.Direction);
                if (tcpConnections.TryRemove(connectId, out AsyncUserToken token))
                {
                    CloseClientSocket(token);
                }
            }
        }

        /// <summary>
        /// 从隧道收到数据，确定是tcp，那就发给对应的socket
        /// </summary>
        /// <param name="tunnelToken"></param>
        /// <returns></returns>
        private async Task SendToSocketTcp(AsyncUserTunnelToken tunnelToken)
        {
            ConnectId connectId = new ConnectId(tunnelToken.Proxy.ConnectId, tunnelToken.Connection.GetHashCode(), (byte)tunnelToken.Proxy.Direction);
            if (tunnelToken.Proxy.Step == ProxyStep.Close || tunnelToken.Proxy.Data.Length == 0)
            {
                if (tcpConnections.TryRemove(connectId, out AsyncUserToken token))
                {
                    CloseClientSocket(token);
                }
                return;
            }
            if (tcpConnections.TryGetValue(connectId, out AsyncUserToken token1) && token1.Socket.Connected)
            {
                try
                {

                    await token1.Socket.SendAsync(tunnelToken.Proxy.Data, SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromMilliseconds(1000)).ConfigureAwait(false);

                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(ex);
                    }

                    await SendToConnectionClose(token1).ConfigureAwait(false);
                    CloseClientSocket(token1);
                }
            }
        }

        private void CloseClientSocket(AsyncUserToken token)
        {
            if (token == null) return;
            if (token.Connection != null)
            {
                tcpConnections.TryRemove(new ConnectId(token.Proxy.ConnectId, token.Connection.GetHashCode(), (byte)token.Proxy.Direction), out _);
            }
            token.Clear();
        }
        private void CloseClientSocketTcp(ITunnelConnection connection)
        {
            int hashcode = connection.GetHashCode();
            var tokens = tcpConnections.Where(c => c.Key.hashcode == hashcode).ToList();
            foreach (var item in tokens)
            {
                try
                {
                    if (tcpConnections.TryRemove(item.Key, out AsyncUserToken token))
                    {
                        token.Clear();
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public void StopTcp()
        {
            foreach (var item in tcpListens)
            {
                CloseClientSocket(item.Value);
            }
            tcpListens.Clear();
            foreach (var item in tcpConnections)
            {
                item.Value?.Socket?.SafeClose();
            }
            tcpConnections.Clear();
        }
        public void StopTcp(int port)
        {
            if (tcpListens.TryRemove(port, out AsyncUserToken userToken))
            {
                CloseClientSocket(userToken);
            }
            if (tcpListens.Count == 0)
            {
                foreach (var item in tcpConnections)
                {
                    item.Value.Clear();
                }
                tcpConnections.Clear();
            }
        }

    }


    public sealed class ConnectIdComparer : IEqualityComparer<ConnectId>
    {
        public bool Equals(ConnectId x, ConnectId y)
        {
            return x.connectId == y.connectId && x.hashcode == y.hashcode && x.direction == y.direction;
        }
        public int GetHashCode(ConnectId obj)
        {
            return obj.connectId.GetHashCode() ^ obj.hashcode ^ obj.direction;
        }
    }
    public record struct ConnectId
    {
        public ulong connectId;
        public int hashcode;
        public byte direction;

        public ConnectId(ulong connectId, int hashcode, byte direction)
        {
            this.connectId = connectId;
            this.hashcode = hashcode;
            this.direction = direction;
        }
    }
    public sealed class AsyncUserToken
    {
        public int ListenPort { get; set; }
        public Socket Socket { get; set; }
        public ITunnelConnection Connection { get; set; }
        public ProxyInfo Proxy { get; set; }
        public SocketAsyncEventArgs Saea { get; set; }

        public bool Received { get; set; } = false;
        public bool Paused { get; set; } = true;

        public uint TargetIP { get; set; }

        public void Clear()
        {
            Socket?.SafeClose();

            Saea?.Dispose();

            GC.Collect();
        }
    }
    public sealed class ConnectState
    {
        public ITunnelConnection Connection { get; set; }
        public ulong ConnectId { get; set; }
        public Socket Socket { get; set; }
        public IPEndPoint IPEndPoint { get; set; }

        public byte[] Data { get; set; } = Helper.EmptyArray;
        public int Length { get; set; }

        public void CopyData(ReadOnlyMemory<byte> data)
        {
            if (data.Length > 0)
            {
                Data = ArrayPool<byte>.Shared.Rent(data.Length);
                Length = data.Length;

                data.CopyTo(Data);
            }
        }

        public void ClearData()
        {
            if (Length > 0)
            {
                ArrayPool<byte>.Shared.Return(Data);
                Data = Helper.EmptyArray;
                Length = 0;
            }
        }
    }

}
