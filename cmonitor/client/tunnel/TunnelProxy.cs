using common.libs;
using common.libs.extends;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.client.tunnel
{
    public class TunnelProxy : ITunnelConnectionReceiveCallback
    {
        private ConcurrentDictionary<int, AsyncUserToken> userTokens = new ConcurrentDictionary<int, AsyncUserToken>();
        private ConcurrentDictionary<int, AsyncUserUdpToken> udpClients = new ConcurrentDictionary<int, AsyncUserUdpToken>();

        private Socket socket;

        private readonly NumberSpace ns = new NumberSpace();
        private readonly ConcurrentDictionary<ConnectId, AsyncUserToken> dic = new ConcurrentDictionary<ConnectId, AsyncUserToken>();
        private ConcurrentDictionary<ConnectIdUdp, AsyncUserUdpTokenTarget> dicUdp = new(new ConnectIdUdpComparer());

        public IPEndPoint LocalEndpoint => socket?.LocalEndPoint as IPEndPoint ?? new IPEndPoint(IPAddress.Any, 0);

        public TunnelProxy()
        {
        }

        public void Start(int port)
        {
            try
            {
                //Stop();

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


                UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, localEndPoint.Port));
                AsyncUserUdpToken asyncUserUdpToken = new AsyncUserUdpToken
                {
                    ListenPort = port,
                    SourceSocket = udpClient,
                    Proxy = new ProxyInfo { Step = ProxyStep.Forward, ConnectId = 0, Protocol = ProxyProtocol.Udp, Direction = ProxyDirection.Forward }
                };
                udpClient.Client.EnableBroadcast = true;
                udpClient.Client.WindowsUdpBug();
                IAsyncResult result = udpClient.BeginReceive(ReceiveCallbackUdp, asyncUserUdpToken);


                userTokens.AddOrUpdate(port, userToken, (a, b) => userToken);
                udpClients.AddOrUpdate(port, asyncUserUdpToken, (a, b) => asyncUserUdpToken);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        private async void ReceiveCallbackUdp(IAsyncResult result)
        {
            try
            {
                AsyncUserUdpToken token = result.AsyncState as AsyncUserUdpToken;

                byte[] bytes = token.SourceSocket.EndReceive(result, ref token.TempRemoteEP);

                token.Proxy.SourceEP = token.TempRemoteEP;
                token.Proxy.Data = bytes;
                await ConnectUdp(token);
                if (token.Connection != null && token.Proxy.TargetEP != null)
                {
                    //发送连接请求包
                    await SendToConnection(token).ConfigureAwait(false);
                }

                result = token.SourceSocket.BeginReceive(ReceiveCallbackUdp, null);
            }
            catch (Exception)
            {
            }
        }
        /// <summary>
        /// 连接UDP
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected virtual async Task ConnectUdp(AsyncUserUdpToken token)
        {
            await Task.CompletedTask;
        }
        private async Task SendToConnection(AsyncUserUdpToken token)
        {
            byte[] connectData = token.Proxy.ToBytes(out int length);
            try
            {
                await token.Connection.SendAsync(connectData.AsMemory(0, length)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                CloseClientSocket(token);
            }
            finally
            {
                token.Proxy.Return(connectData);
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
                BindReceive(e);
                StartAccept(e);
            }
        }
        private void BindReceive(SocketAsyncEventArgs e)
        {
            try
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                var socket = e.AcceptSocket;

                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return;
                }

                socket.KeepAlive();
                AsyncUserToken userToken = new AsyncUserToken
                {
                    Socket = socket,
                    ListenPort = token.ListenPort,
                    Proxy = new ProxyInfo { Data = Helper.EmptyArray, Step = ProxyStep.Request, ConnectId = ns.Increment() }
                };

                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = userToken,
                    SocketFlags = SocketFlags.None,
                };
                userToken.Saea = readEventArgs;

                readEventArgs.SetBuffer(new byte[8 * 1024], 0, 8 * 1024);
                readEventArgs.Completed += IO_Completed;
                if (socket.ReceiveAsync(readEventArgs) == false)
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

                    if (token.Received == false) return;

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
                                CloseClientSocket(token);
                                return;
                            }
                            if (token.Received == false) return;
                        }
                    }

                    if (token.Socket.Connected == false)
                    {
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
                    CloseClientSocket(token);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
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
                    dic.TryAdd(new ConnectId(token.Proxy.ConnectId, token.Connection.GetHashCode()), token);
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


        protected void BindConnectionReceive(ITunnelConnection connection)
        {
            connection.BeginReceive(this, new AsyncUserTunnelToken
            {
                Connection = connection,
                Proxy = new ProxyInfo { }
            });
        }
        public async Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> memory, object userToken)
        {
            AsyncUserTunnelToken token = userToken as AsyncUserTunnelToken;
            token.Proxy.DeBytes(memory);
            await ReadConnectionPack(token).ConfigureAwait(false);
        }
        public async Task Closed(ITunnelConnection connection, object userToken)
        {
            CloseClientSocket(userToken as AsyncUserToken);
            await Task.CompletedTask;
        }
        private async Task ReadConnectionPack(AsyncUserTunnelToken token)
        {
            switch (token.Proxy.Step)
            {
                case ProxyStep.Request:
                    ConnectBind(token);
                    break;
                case ProxyStep.Forward:
                    await SendToSocket(token).ConfigureAwait(false);
                    break;
                case ProxyStep.Receive:
                    ReceiveSocket(token);
                    break;
                case ProxyStep.Close:
                    CloseSocket(token);
                    break;
                default:
                    break;
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
                state.Socket.EndConnect(result);
                dic.TryAdd(new ConnectId(state.ConnectId, state.Connection.GetHashCode()), token);
                await SendToConnection(token);

                token.Proxy.Step = ProxyStep.Forward;

                if (state.Data.Length > 0)
                {
                    await state.Socket.SendAsync(state.Data.AsMemory(0, state.Length), SocketFlags.None);
                }
                BindReceiveTarget(token);
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(state.IPEndPoint.ToString());
                    Logger.Instance.Error(ex);
                }
                token.Proxy.Step = ProxyStep.Close;
                await SendToConnection(token);
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
                ConnectId connectId = new ConnectId(tunnelToken.Proxy.ConnectId, tunnelToken.Connection.GetHashCode());
                if (dic.TryGetValue(connectId, out AsyncUserToken token))
                {
                    if (token.Received == false)
                    {
                        token.Received = true;
                        if (token.Socket.ReceiveAsync(token.Saea) == false)
                        {
                            ProcessReceive(token.Saea);
                        }
                    }
                }
            }
        }
        private void CloseSocket(AsyncUserTunnelToken tunnelToken)
        {
            if (tunnelToken.Proxy.Protocol == ProxyProtocol.Tcp)
            {
                ConnectId connectId = new ConnectId(tunnelToken.Proxy.ConnectId, tunnelToken.Connection.GetHashCode());
                if (dic.TryRemove(connectId, out AsyncUserToken token))
                {
                    CloseClientSocket(token);
                }
            }
        }
        private async Task SendToSocket(AsyncUserTunnelToken tunnelToken)
        {
            if (tunnelToken.Proxy.Protocol == ProxyProtocol.Tcp)
            {
                await SendToSocketTcp(tunnelToken).ConfigureAwait(false);
            }
            else
            {
                await SendToSocketUdp(tunnelToken).ConfigureAwait(false);
            }
        }
        private async Task SendToSocketTcp(AsyncUserTunnelToken tunnelToken)
        {
            ConnectId connectId = new ConnectId(tunnelToken.Proxy.ConnectId, tunnelToken.Connection.GetHashCode());
            if (tunnelToken.Proxy.Data.Length == 0)
            {
                if (dic.TryRemove(connectId, out AsyncUserToken token))
                {
                    CloseClientSocket(token);
                }
                return;
            }

            if (dic.TryGetValue(connectId, out AsyncUserToken token1) && token1.Socket.Connected)
            {
                try
                {
                    await token1.Socket.SendAsync(tunnelToken.Proxy.Data);
                }
                catch (Exception)
                {
                    token1.Proxy.Step = ProxyStep.Close;
                    token1.Proxy.Data = Helper.EmptyArray;
                    await SendToConnection(token1);
                    CloseClientSocket(token1);
                }
            }
            else if (tunnelToken.Proxy.Direction == ProxyDirection.Forward)
            {
                //ConnectBind(tunnelToken);
            }
        }
        private async Task SendToSocketUdp(AsyncUserTunnelToken tunnelToken)
        {
            if (tunnelToken.Proxy.Direction == ProxyDirection.Forward)
            {
                ConnectIdUdp connectId = new ConnectIdUdp(tunnelToken.Proxy.ConnectId, tunnelToken.Proxy.SourceEP, tunnelToken.Connection.GetHashCode());
                try
                {

                    if (dicUdp.TryGetValue(connectId, out AsyncUserUdpTokenTarget token))
                    {
                        await token.TargetSocket.SendToAsync(tunnelToken.Proxy.Data, tunnelToken.Proxy.TargetEP);
                        return;
                    }

                    socket = new Socket(tunnelToken.Proxy.TargetEP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    socket.WindowsUdpBug();
                    AsyncUserUdpTokenTarget udpToken = new AsyncUserUdpTokenTarget
                    {
                        Proxy = new ProxyInfo
                        {
                            ConnectId = tunnelToken.Proxy.ConnectId,
                            Direction = ProxyDirection.Reverse,
                            Protocol = tunnelToken.Proxy.Protocol,
                            SourceEP = tunnelToken.Proxy.SourceEP,
                            TargetEP = tunnelToken.Proxy.TargetEP,
                            Step = tunnelToken.Proxy.Step,
                        },
                        TargetSocket = socket,
                        ConnectId = connectId,
                        Connection = tunnelToken.Connection
                    };
                    udpToken.Proxy.Direction = ProxyDirection.Reverse;
                    udpToken.PoolBuffer = new byte[65535];
                    dicUdp.AddOrUpdate(connectId, udpToken, (a, b) => udpToken);

                    await udpToken.TargetSocket.SendToAsync(tunnelToken.Proxy.Data, SocketFlags.None, tunnelToken.Proxy.TargetEP);
                    IAsyncResult result = socket.BeginReceiveFrom(udpToken.PoolBuffer, 0, udpToken.PoolBuffer.Length, SocketFlags.None, ref udpToken.TempRemoteEP, ReceiveCallbackUdpTarget, udpToken);
                }
                catch (Exception)
                {
                    if (dicUdp.TryRemove(connectId, out AsyncUserUdpTokenTarget token))
                    {
                        CloseClientSocket(token);
                    }
                }
            }
            else
            {
                if (udpClients.TryGetValue(tunnelToken.Proxy.Port, out AsyncUserUdpToken asyncUserUdpToken))
                {
                    try
                    {
                        if (await ConnectionReceiveUdp(tunnelToken, asyncUserUdpToken) == false)
                        {
                            await asyncUserUdpToken.SourceSocket.SendAsync(tunnelToken.Proxy.Data, tunnelToken.Proxy.SourceEP);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
        /// <summary>
        /// 连接对方返回UDP，是否要自己处理
        /// </summary>
        /// <param name="token"></param>
        /// <param name="asyncUserUdpToken"></param>
        /// <returns>true表示自己已经处理过了，不需要再处理了</returns>
        protected virtual async Task<bool> ConnectionReceiveUdp(AsyncUserTunnelToken token, AsyncUserUdpToken asyncUserUdpToken)
        {
            return await Task.FromResult(false);
        }


        private async void ReceiveCallbackUdpTarget(IAsyncResult result)
        {
            AsyncUserUdpTokenTarget token = result.AsyncState as AsyncUserUdpTokenTarget;
            try
            {
                int length = token.TargetSocket.EndReceiveFrom(result, ref token.TempRemoteEP);

                if (length > 0)
                {
                    token.Proxy.Data = token.PoolBuffer.AsMemory(0, length);

                    token.Update();
                    await SendToConnection(token);
                    token.Proxy.Data = Helper.EmptyArray;
                }
                result = token.TargetSocket.BeginReceiveFrom(token.PoolBuffer, 0, token.PoolBuffer.Length, SocketFlags.None, ref token.TempRemoteEP, ReceiveCallbackUdp, token);
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error($"forward udp -> receive" + ex);
                }
                CloseClientSocket(token);
            }
        }
        private async Task SendToConnection(AsyncUserUdpTokenTarget token)
        {
            byte[] connectData = token.Proxy.ToBytes(out int length);
            try
            {
                await token.Connection.SendAsync(connectData.AsMemory(0, length)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                CloseClientSocket(token);
            }
            finally
            {
                token.Proxy.Return(connectData);
            }
        }


        private void IO_CompletedTarget(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceiveTarget(e);
                    break;
                default:
                    break;
            }
        }
        private void BindReceiveTarget(AsyncUserToken userToken)
        {
            try
            {
                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = userToken,
                    SocketFlags = SocketFlags.None,
                };
                readEventArgs.SetBuffer(new byte[8 * 1024], 0, 8 * 1024);
                readEventArgs.Completed += IO_CompletedTarget;
                if (userToken.Socket.ReceiveAsync(readEventArgs) == false)
                {
                    ProcessReceiveTarget(readEventArgs);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }
        }
        private async void ProcessReceiveTarget(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int offset = e.Offset;
                    int length = e.BytesTransferred;

                    token.Proxy.Data = e.Buffer.AsMemory(offset, length);
                    await SendToConnection(token).ConfigureAwait(false);

                    if (token.Socket.Available > 0)
                    {
                        while (token.Socket.Available > 0)
                        {
                            length = token.Socket.Receive(e.Buffer);

                            if (length > 0)
                            {
                                token.Proxy.Data = e.Buffer.AsMemory(0, length);
                                await SendToConnection(token).ConfigureAwait(false);
                            }
                            else
                            {
                                token.Proxy.Step = ProxyStep.Close;
                                token.Proxy.Data = Helper.EmptyArray;
                                await SendToConnection(token).ConfigureAwait(false);
                                CloseClientSocket(token);
                                return;
                            }
                        }
                    }

                    if (token.Connection.Connected == false)
                    {
                        token.Proxy.Step = ProxyStep.Close;
                        token.Proxy.Data = Helper.EmptyArray;
                        await SendToConnection(token).ConfigureAwait(false);
                        CloseClientSocket(token);
                        return;
                    }

                    if (token.Socket.ReceiveAsync(e) == false)
                    {
                        ProcessReceiveTarget(e);
                    }
                }
                else
                {
                    token.Proxy.Step = ProxyStep.Close;
                    token.Proxy.Data = Helper.EmptyArray;
                    await SendToConnection(token).ConfigureAwait(false);
                    CloseClientSocket(token);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
                token.Proxy.Step = ProxyStep.Close;
                token.Proxy.Data = Helper.EmptyArray;
                await SendToConnection(token).ConfigureAwait(false);
                CloseClientSocket(token);
            }
        }

        private void CloseClientSocket(AsyncUserToken token)
        {
            if (token == null) return;
            if (token.Connection != null)
            {
                dic.TryRemove(new ConnectId(token.Proxy.ConnectId, token.Connection.GetHashCode()), out _);
            }
            token.Clear();
        }
        private void CloseClientSocket(AsyncUserUdpToken token)
        {
            if (token == null) return;
            token.Clear();
        }
        private void CloseClientSocket(AsyncUserUdpTokenTarget token)
        {
            if (token == null) return;
            if (dicUdp.TryRemove(token.ConnectId, out _))
            {
                token.Clear();
            }
            token.Clear();
        }

        public virtual void Stop()
        {
            foreach (var item in userTokens)
            {
                CloseClientSocket(item.Value);
            }
            userTokens.Clear();
            foreach (var item in udpClients)
            {
                item.Value.Clear();
            }
            udpClients.Clear();

            foreach (var item in dic)
            {
                item.Value?.Socket?.SafeClose();
            }
            dic.Clear();

            foreach (var item in dicUdp)
            {
                item.Value?.TargetSocket?.SafeClose();
            }
            dicUdp.Clear();
        }
        public virtual void Stop(int port)
        {
            if (userTokens.TryRemove(port, out AsyncUserToken userToken))
            {
                CloseClientSocket(userToken);
            }
            if (udpClients.TryRemove(port, out AsyncUserUdpToken udpClient))
            {
                udpClient.Clear();
            }

            if (userTokens.Count == 0 && udpClients.Count == 0)
            {
                foreach (var item in dic)
                {
                    item.Value?.Socket?.SafeClose();
                }
                dic.Clear();

                foreach (var item in dicUdp)
                {
                    item.Value?.TargetSocket?.SafeClose();
                }
                dicUdp.Clear();
            }
        }

    }

    public enum ProxyStep : byte
    {
        Request = 1,
        Forward = 2,
        Receive = 4,
        Close = 8,
    }
    public enum ProxyProtocol : byte
    {
        Tcp = 0,
        Udp = 1
    }
    public enum ProxyDirection : byte
    {
        Forward = 0,
        Reverse = 1
    }

    public sealed class ProxyInfo
    {
        public ulong ConnectId { get; set; }
        public ProxyStep Step { get; set; } = ProxyStep.Request;
        public ProxyProtocol Protocol { get; set; } = ProxyProtocol.Tcp;
        public ProxyDirection Direction { get; set; } = ProxyDirection.Forward;

        public ushort Port { get; set; }
        public IPEndPoint SourceEP { get; set; }
        public IPEndPoint TargetEP { get; set; }

        public byte Rsv { get; set; }

        public ReadOnlyMemory<byte> Data { get; set; }

        public byte[] ToBytes(out int length)
        {
            int sourceLength = SourceEP == null ? 0 : (SourceEP.AddressFamily == AddressFamily.InterNetwork ? 4 : 16) + 2;
            int targetLength = TargetEP == null ? 0 : (TargetEP.AddressFamily == AddressFamily.InterNetwork ? 4 : 16) + 2;

            length = 4 + 8 + 1 + 1 + 1
                + 2
                + 1 + sourceLength
                + 1 + targetLength
                + Data.Length;

            byte[] bytes = ArrayPool<byte>.Shared.Rent(length);
            Memory<byte> memory = bytes.AsMemory();

            int index = 0;

            (length - 4).ToBytes(memory);
            index += 4;


            ConnectId.ToBytes(memory.Slice(index));
            index += 8;

            bytes[index] = (byte)Step;
            index += 1;

            bytes[index] = (byte)Protocol;
            index += 1;

            bytes[index] = (byte)Direction;
            index += 1;

            Port.ToBytes(memory.Slice(index));
            index += 2;

            bytes[index] = (byte)sourceLength;
            index += 1;

            if (sourceLength > 0)
            {
                SourceEP.Address.TryWriteBytes(memory.Slice(index).Span, out int writeLength);
                index += writeLength;

                ((ushort)SourceEP.Port).ToBytes(memory.Slice(index));
                index += 2;
            }


            bytes[index] = (byte)targetLength;
            index += 1;

            if (targetLength > 0)
            {
                TargetEP.Address.TryWriteBytes(memory.Slice(index).Span, out int writeLength);
                index += writeLength;

                ((ushort)TargetEP.Port).ToBytes(memory.Slice(index));
                index += 2;
            }

            Data.CopyTo(memory.Slice(index));

            return bytes;

        }

        public void Return(byte[] bytes)
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }

        public void DeBytes(ReadOnlyMemory<byte> memory)
        {
            int index = 0;
            ReadOnlySpan<byte> span = memory.Span;

            ConnectId = memory.Slice(index).ToUInt64();
            index += 8;

            Step = (ProxyStep)span[index];
            index += 1;

            Protocol = (ProxyProtocol)span[index];
            index += 1;

            Direction = (ProxyDirection)span[index];
            index += 1;

            Port = memory.Slice(index).ToUInt16();
            index += 2;

            byte sourceLength = span[index];
            index += 1;
            if (sourceLength > 0)
            {
                IPAddress ip = new IPAddress(span.Slice(index, sourceLength - 2));
                index += sourceLength;
                ushort port = span.Slice(index - 2).ToUInt16();
                SourceEP = new IPEndPoint(ip, port);
            }

            byte targetLength = span[index];
            index += 1;
            if (targetLength > 0)
            {
                IPAddress ip = new IPAddress(span.Slice(index, targetLength - 2));
                index += targetLength;
                ushort port = span.Slice(index - 2).ToUInt16();
                TargetEP = new IPEndPoint(ip, port);
            }
            Data = memory.Slice(index);
        }
    }

    public sealed class AsyncUserTunnelToken
    {
        public ITunnelConnection Connection { get; set; }

        public ProxyInfo Proxy { get; set; }

        public void Clear()
        {
            GC.Collect();
        }
    }

    public record struct ConnectId
    {
        public ulong connectId;
        public int hashCode;

        public ConnectId(ulong connectId, int hashCode)
        {
            this.connectId = connectId;
            this.hashCode = hashCode;
        }
    }
    public sealed class AsyncUserToken
    {
        public int ListenPort { get; set; }
        public Socket Socket { get; set; }
        public ITunnelConnection Connection { get; set; }

        public ProxyInfo Proxy { get; set; }

        public SocketAsyncEventArgs Saea { get; set; }

        public bool Received { get; set; }

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

    public sealed class AsyncUserUdpToken
    {
        public int ListenPort { get; set; }
        public UdpClient SourceSocket { get; set; }
        public ITunnelConnection Connection { get; set; }
        public ProxyInfo Proxy { get; set; }

        public IPEndPoint TempRemoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);

        public void Clear()
        {
            SourceSocket?.Close();
            SourceSocket = null;
            GC.Collect();
        }
    }
    public sealed class AsyncUserUdpTokenTarget
    {
        public Socket TargetSocket { get; set; }
        public byte[] PoolBuffer { get; set; }

        public ITunnelConnection Connection { get; set; }
        public ProxyInfo Proxy { get; set; }

        public ConnectIdUdp ConnectId { get; set; }

        public int LastTime { get; set; } = Environment.TickCount;
        public EndPoint TempRemoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
        public void Clear()
        {
            TargetSocket?.SafeClose();
            PoolBuffer = Helper.EmptyArray;
            GC.Collect();
            GC.SuppressFinalize(this);
        }
        public void Update()
        {
            LastTime = Environment.TickCount;
        }
    }

    public sealed class ConnectIdUdpComparer : IEqualityComparer<ConnectIdUdp>
    {
        public bool Equals(ConnectIdUdp x, ConnectIdUdp y)
        {
            return x.Source != null && x.Source.Equals(y.Source) && x.ConnectId == y.ConnectId && x.HashCode == y.HashCode;
        }
        public int GetHashCode(ConnectIdUdp obj)
        {
            if (obj.Source == null) return 0;
            return obj.Source.GetHashCode() ^ obj.ConnectId.GetHashCode() ^ obj.HashCode;
        }
    }
    public readonly struct ConnectIdUdp
    {
        public readonly IPEndPoint Source { get; }
        public readonly ulong ConnectId { get; }
        public int HashCode { get; }

        public ConnectIdUdp(ulong connectId, IPEndPoint source, int hashCode)
        {
            ConnectId = connectId;
            Source = source;
            HashCode = hashCode;
        }
    }
}
