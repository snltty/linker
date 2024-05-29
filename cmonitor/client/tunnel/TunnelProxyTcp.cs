using common.libs;
using common.libs.extends;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.client.tunnel
{
    public partial class TunnelProxy
    {
        private ConcurrentDictionary<int, AsyncUserToken> tcpListens = new ConcurrentDictionary<int, AsyncUserToken>();
        private readonly ConcurrentDictionary<ConnectId, AsyncUserToken> tcpConnections = new ConcurrentDictionary<ConnectId, AsyncUserToken>();
        private Socket socket;
        public IPEndPoint LocalEndpoint => socket?.LocalEndPoint as IPEndPoint ?? new IPEndPoint(IPAddress.Any, 0);


        private void StartTcp(int port)
        {
            try
            {
                IPEndPoint localEndPoint = new IPEndPoint(NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any, port);
                socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.IPv6Only(localEndPoint.AddressFamily, false);
                socket.ReuseBind(localEndPoint);
                socket.Listen(int.MaxValue);
                AsyncUserToken userToken = new AsyncUserToken
                {
                    ListenPort = port,
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

                tcpListens.AddOrUpdate(port, userToken, (a, b) => userToken);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
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
            catch (Exception)
            {
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
                        Proxy = new ProxyInfo { Data = Helper.EmptyArray, Step = ProxyStep.Request, Port = acceptToken.ListenPort, ConnectId = ns.Increment() }
                    };
                    BindReceive(userToken);
                }
                StartAccept(e);
            }
        }
        private void BindReceive(AsyncUserToken token)
        {
            try
            {
                token.SenderPipe = new Pipe(new PipeOptions(pauseWriterThreshold: 1 * 1024 * 1024, resumeWriterThreshold: 64 * 1024));
                _ = ProcessSender(token);

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
        private async Task ReadPacket(AsyncUserToken token, Memory<byte> data)
        {
            token.Proxy.Data = data;
            if (token.Proxy.Step == ProxyStep.Request)
            {
                bool closeConnect = await ConnectTcp(token);
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
        protected virtual async Task<bool> ConnectTcp(AsyncUserToken token)
        {
            return await Task.FromResult(false);
        }
        private async Task SendToConnection(AsyncUserToken token)
        {
            byte[] connectData = token.Proxy.ToBytes(out int length);
            try
            {
                await token.Connection.SendAsync(connectData.AsMemory(0, length)).ConfigureAwait(false);
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
            }
        }
        private async Task SendToConnectionClose(AsyncUserToken token)
        {
            if (token.Proxy.Direction == ProxyDirection.Reverse)
            {
                ProxyStep step = token.Proxy.Step;
                token.Proxy.Step = ProxyStep.Close;
                token.Proxy.Data = Helper.EmptyArray;
                await SendToConnection(token);
                token.Proxy.Step = step;
            }
        }

        private void ConnectBind(AsyncUserTunnelToken token)
        {
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
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(state.IPEndPoint.ToString());
                    Logger.Instance.Error(ex);
                }
                await SendToConnectionClose(token).ConfigureAwait(false);
                CloseClientSocket(token);
            }
            finally
            {
                state.ClearData();
            }
        }

        private async Task ProcessSender(AsyncUserToken token)
        {
            PipeReader reader = token.SenderPipe.Reader;
            try
            {
                while (true)
                {
                    ReadResult readResult = await reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = readResult.Buffer;
                    if (buffer.IsEmpty && readResult.IsCompleted)
                    {
                        break;
                    }
                    if (buffer.Length > 0)
                    {
                        foreach (ReadOnlyMemory<byte> memory in buffer)
                        {
                            await token.Socket.SendAsync(memory, SocketFlags.None);
                        }
                        reader.AdvanceTo(buffer.End);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
            finally
            {
                await SendToConnectionClose(token).ConfigureAwait(false);
                CloseClientSocket(token);
            }
        }

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

        private async Task SendToSocketTcp(AsyncUserTunnelToken tunnelToken)
        {
            ConnectId connectId = new ConnectId(tunnelToken.Proxy.ConnectId, tunnelToken.Connection.GetHashCode(), (byte)tunnelToken.Proxy.Direction);
            if (tunnelToken.Proxy.Data.Length == 0)
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
                    token1.SenderPipe.Writer.Write(tunnelToken.Proxy.Data.Span);
                    await token1.SenderPipe.Writer.FlushAsync();
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

    public record struct ConnectId
    {
        public ulong connectId;
        public int hashCode;
        public byte direction;

        public ConnectId(ulong connectId, int hashCode, byte direction)
        {
            this.connectId = connectId;
            this.hashCode = hashCode;
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

        public Pipe SenderPipe { get; set; }

        public bool Received { get; set; } = false;
        public bool Paused { get; set; } = true;

        public void Clear()
        {
            Socket?.SafeClose();

            Saea?.Dispose();

            SenderPipe?.Writer.Complete();
            SenderPipe?.Reader.Complete();

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
