using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace linker.plugins.socks5
{
    public partial class TunnelProxy
    {
        private ConcurrentDictionary<int, AsyncUserToken> tcpListens = new ConcurrentDictionary<int, AsyncUserToken>();
        private readonly ConcurrentDictionary<ConnectId, AsyncUserToken> tcpConnections = new ConcurrentDictionary<ConnectId, AsyncUserToken>(new ConnectIdComparer());
        private AsyncUserToken userToken;
        public IPEndPoint LocalEndpoint { get; private set; }

        public bool Running
        {
            get
            {
                return userToken != null && userToken.ListenPort > 0;
            }
        }
        public string Error { get; private set; }

        private void StartTcp(IPEndPoint ep, byte bufferSize)
        {
            Error = string.Empty;

            try
            {
                IPEndPoint _localEndPoint = ep;
                userToken = new AsyncUserToken
                {
                    ListenPort = _localEndPoint.Port,
                    Socket = new Socket(_localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp),
                    BufferSize = bufferSize
                };

                userToken.Socket.IPv6Only(_localEndPoint.AddressFamily, false);
                userToken.Socket.ReuseBind(_localEndPoint);
                userToken.Socket.Listen(int.MaxValue);

                LocalEndpoint = userToken.Socket.LocalEndPoint as IPEndPoint;

                _ = StartAccept(userToken);
                tcpListens.AddOrUpdate(LocalEndpoint.Port, userToken, (a, b) => userToken);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                LoggerHelper.Instance.Error(ex);
            }
        }

        private async Task StartAccept(AsyncUserToken token)
        {
            try
            {
                while (true)
                {
                    Socket socket = await token.Socket.AcceptAsync().ConfigureAwait(false);
                    ProcessAccept(token, socket);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
                token.Clear();
            }

        }
        private void ProcessAccept(AsyncUserToken acceptToken, Socket socket)
        {
            try
            {
                if (socket != null && socket.RemoteEndPoint != null)
                {
                    socket.KeepAlive();
                    AsyncUserToken userToken = new AsyncUserToken
                    {
                        Socket = socket,
                        ListenPort = acceptToken.ListenPort,
                        BufferSize = acceptToken.BufferSize,
                        Buffer = new byte[(1 << acceptToken.BufferSize) * 1024],
                        Proxy = new ProxyInfo { Data = Helper.EmptyArray, Step = ProxyStep.Request, Port = (ushort)acceptToken.ListenPort, ConnectId = ns.Increment() }
                    };
                    _ = BeginReceive(userToken);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
        }
        private async Task BeginReceive(AsyncUserToken token)
        {
            bool closeConnect = await ConnectTunnelConnection(token).ConfigureAwait(false);
            if (token.Connection != null)
            {
                if (token.Proxy.TargetEP != null)
                {
                    await SendToConnection(token).ConfigureAwait(false);
                }
                token.Proxy.Step = ProxyStep.Forward;
                //绑定
                tcpConnections.TryAdd(token.GetConnectId(ProxyDirection.Reverse), token);
            }
            else if (closeConnect)
            {
                CloseClientSocket(token, 7);
            }
            else
            {
                _ = ProcessReceive(token, false);
            }
        }

        private async Task ProcessReceive(AsyncUserToken token, bool send = true)
        {
            if (token.Received) return;
            token.Received = true;

            try
            {
                while (true)
                {
                    int length = await token.Socket.ReceiveAsync(token.Buffer.AsMemory(), SocketFlags.None).ConfigureAwait(false);
                    if (length == 0)
                    {
                        break;
                    }

                    token.Proxy.Data = token.Buffer.AsMemory(0, length);
                    if (send)
                        await SendToConnection(token).ConfigureAwait(false);

                    while (token.Socket.Available > 0)
                    {
                        length = token.Socket.Receive(token.Buffer);
                        if (length == 0)
                        {
                            break;
                        }
                        token.Proxy.Data = token.Buffer.AsMemory(0, length);
                        if (send)
                            await SendToConnection(token).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                if (send)
                    await SendToConnectionClose(token).ConfigureAwait(false);
                CloseClientSocket(token, 6);
            }

        }

        private async Task SendToConnection(AsyncUserToken token)
        {
            if (token.Connection == null)
            {
                return;
            }

            byte[] connectData = token.Proxy.ToBytes(out int length);
            try
            {
                bool res = await token.Connection.SendAsync(connectData.AsMemory(0, length)).ConfigureAwait(false);
                if (res == false)
                {
                    CloseClientSocket(token, 5);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
            }
            token.Proxy.Return(connectData);
        }

        private async Task SendToConnectionClose(AsyncUserToken token)
        {
            //if (token.Proxy.Direction == ProxyDirection.Reverse)
            {
                ProxyStep step = token.Proxy.Step;
                token.Proxy.Step = ProxyStep.Close;
                token.Proxy.Data = Helper.EmptyArray;
                await SendToConnection(token).ConfigureAwait(false);
                token.Proxy.Step = step;
            }
        }


        private void ConnectBind(AsyncUserTunnelToken token)
        {
            if (token.Proxy.TargetEP == null) return;

            Socket socket = new Socket(token.Proxy.TargetEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.KeepAlive();

            ConnectState state = new ConnectState { BufferSize = token.Proxy.BufferSize, Connection = token.Connection, ConnectId = token.Proxy.ConnectId, Socket = socket, IPEndPoint = token.Proxy.TargetEP };
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
                Buffer = new byte[(1 << state.BufferSize) * 1024],
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
                    await token.Socket.SendAsync(state.Data.AsMemory(0, state.Length), SocketFlags.None).ConfigureAwait(false);
                }
                tcpConnections.TryAdd(token.GetConnectId(ProxyDirection.Forward), token);

                await SendToConnection(token).ConfigureAwait(false);
                token.Proxy.Step = ProxyStep.Forward;

                _ = ProcessReceive(token);
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error($"connect {state.IPEndPoint} error -> {ex}");
                await SendToConnectionClose(token).ConfigureAwait(false);
                CloseClientSocket(token, 4);
            }
            finally
            {
                state.ClearData();
            }
        }


        private void ReceiveSocket(AsyncUserTunnelToken tunnelToken)
        {
            if (tunnelToken.Proxy.Protocol == ProxyProtocol.Tcp)
            {
                ConnectId connectId = tunnelToken.GetTcpConnectId();
                if (tcpConnections.TryGetValue(connectId, out AsyncUserToken token))
                {
                    _ = ProcessReceive(token);
                }
            }
        }
        private void CloseSocket(AsyncUserTunnelToken tunnelToken)
        {
            if (tunnelToken.Proxy.Protocol == ProxyProtocol.Tcp)
            {
                ConnectId connectId = tunnelToken.GetTcpConnectId();
                if (tcpConnections.TryRemove(connectId, out AsyncUserToken token))
                {
                    CloseClientSocket(token, 3);
                }
            }
        }

        private async Task SendToSocketTcp(AsyncUserTunnelToken tunnelToken)
        {
            ConnectId connectId = tunnelToken.GetTcpConnectId();
            if (tunnelToken.Proxy.Step == ProxyStep.Close || tunnelToken.Proxy.Data.Length == 0)
            {
                if (tcpConnections.TryRemove(connectId, out AsyncUserToken token))
                {
                    CloseClientSocket(token, 2);
                }
                return;
            }
            if (tcpConnections.TryGetValue(connectId, out AsyncUserToken token1))
            {
                try
                {
                    token1.Connection = tunnelToken.Connection;
                    await token1.Socket.SendAsync(tunnelToken.Proxy.Data, SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                    await SendToConnectionClose(token1).ConfigureAwait(false);
                    CloseClientSocket(token1, 1);
                }
            }
        }

        private void CloseClientSocket(AsyncUserToken token, int index)
        {
            if (token == null) return;
            if (token.Connection != null)
            {
                tcpConnections.TryRemove(token.GetConnectId(), out _);
            }
            token.Clear();
        }
        private void CloseClientSocketTcp(ITunnelConnection connection)
        {
            int hashcode1 = connection.RemoteMachineId.GetHashCode();
            int hashcode2 = connection.TransactionId.GetHashCode();
            var tokens = tcpConnections.Where(c => c.Key.hashcode1 == hashcode1 && c.Key.hashcode2 == hashcode2).ToList();
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
                CloseClientSocket(item.Value, 0);
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
                CloseClientSocket(userToken, 0);
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
            return x.connectId == y.connectId && x.hashcode1 == y.hashcode1 && x.hashcode2 == y.hashcode2 && x.direction == y.direction;
        }
        public int GetHashCode(ConnectId obj)
        {
            return obj.connectId.GetHashCode() ^ obj.hashcode1 ^ obj.hashcode2 ^ obj.direction;
        }
    }
    public record struct ConnectId
    {
        public ulong connectId;
        public int hashcode1;
        public int hashcode2;
        public byte direction;

        public ConnectId(ulong connectId, int hashcode1, int hashcode2, byte direction)
        {
            this.connectId = connectId;
            this.hashcode1 = hashcode1;
            this.hashcode2 = hashcode2;
            this.direction = direction;
        }
    }
    public sealed class AsyncUserToken
    {
        public int ListenPort { get; set; }
        public Socket Socket { get; set; }
        public ITunnelConnection Connection { get; set; }
        public ProxyInfo Proxy { get; set; }

        public bool Received { get; set; }

        public byte[] Buffer { get; set; }

        public byte BufferSize { get; set; } = 3;


        public void Clear()
        {
            Socket?.SafeClose();

            Buffer = Helper.EmptyArray;

            ListenPort = 0;

            GC.Collect();
        }

        public ConnectId GetConnectId()
        {
            return new ConnectId(Proxy.ConnectId, Connection.RemoteMachineId.GetHashCode(), Connection.TransactionId.GetHashCode(), (byte)Proxy.Direction);
        }
        public ConnectId GetConnectId(ProxyDirection proxyDirection)
        {
            return new ConnectId(Proxy.ConnectId, Connection.RemoteMachineId.GetHashCode(), Connection.TransactionId.GetHashCode(), (byte)proxyDirection);
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

        public byte BufferSize { get; set; } = 3;

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
