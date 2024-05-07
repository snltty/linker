using common.libs;
using common.libs.extends;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.client.tunnel
{
    public class TunnelProxy
    {
        private AsyncUserToken userToken;
        private Socket socket;
        private UdpClient udpClient;
        private readonly NumberSpace ns = new NumberSpace();
        private readonly ConcurrentDictionary<ConnectId, Socket> dic = new ConcurrentDictionary<ConnectId, Socket>();

        public IPEndPoint LocalEndpoint => socket?.LocalEndPoint as IPEndPoint ?? new IPEndPoint(IPAddress.Any, 0);

        public TunnelProxy()
        {
        }

        public void Start(int port)
        {
            try
            {
                Stop();

                IPEndPoint localEndPoint = new IPEndPoint(NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any, port);
                socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.IPv6Only(localEndPoint.AddressFamily, false);
                socket.ReuseBind(localEndPoint);
                socket.Listen(int.MaxValue);

                userToken = new AsyncUserToken
                {
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

                udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, localEndPoint.Port));
                udpClient.Client.EnableBroadcast = true;
                udpClient.Client.WindowsUdpBug();
                IAsyncResult result = udpClient.BeginReceive(ReceiveCallbackUdp, null);

            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }


        private readonly AsyncUserUdpToken asyncUserUdpToken = new AsyncUserUdpToken
        {
            Proxy = new ProxyInfo { Step = ProxyStep.Forward, ConnectId = 0 }
        };
        private async void ReceiveCallbackUdp(IAsyncResult result)
        {
            try
            {
                //System.Net.Quic.QuicListener.IsSupported
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                byte[] bytes = udpClient.EndReceive(result, ref endPoint);

                asyncUserUdpToken.Proxy.Data = bytes;
                await ConnectUdp(asyncUserUdpToken);

                result = udpClient.BeginReceive(ReceiveCallbackUdp, null);
            }
            catch (Exception)
            {
            }
        }
        protected virtual async Task ConnectUdp(AsyncUserUdpToken token)
        {
            await Task.CompletedTask;
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
                    await ReadPacket(token, e.Buffer.AsMemory(offset, length));
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
            if (token.Proxy.Step == ProxyStep.Request)
            {
                await Connect(token);
                if (token.Connection != null)
                {
                    //发送连接请求包
                    await SendToConnection(token).ConfigureAwait(false);

                    token.Proxy.Step = ProxyStep.Forward;
                    token.Proxy.TargetEP = null;

                    //发送后续数据包
                    token.Proxy.Data = data;
                    await SendToConnection(token).ConfigureAwait(false);

                    //绑定
                    dic.TryAdd(new ConnectId(token.Proxy.ConnectId, token.Connection.GetHashCode()), token.Socket);
                }
                else
                {
                    CloseClientSocket(token);
                }
            }
            else
            {
                token.Proxy.Data = data;
                await SendToConnection(token).ConfigureAwait(false);
            }
        }

        protected virtual async Task Connect(AsyncUserToken token)
        {
            await Task.CompletedTask;
        }

        private async Task SendToConnection(AsyncUserToken token)
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

        protected void BindConnectionReceive(ITunnelConnection connection)
        {
            connection.BeginReceive(InputConnectionData, CloseConnection, new AsyncUserToken
            {
                Connection = connection,
                Buffer = new ReceiveDataBuffer(),
                Proxy = new ProxyInfo { }
            });
        }
        protected async Task InputConnectionData(ITunnelConnection connection, Memory<byte> memory, object userToken)
        {
            AsyncUserToken token = userToken as AsyncUserToken;
            //是一个完整的包
            if (token.Buffer.Size == 0 && memory.Length > 4)
            {
                int packageLen = memory.ToInt32();
                if (packageLen == memory.Length - 4)
                {
                    token.Proxy.DeBytes(memory.Slice(0, packageLen + 4));
                    await ReadConnectionPack(token).ConfigureAwait(false);
                    return;
                }
            }

            //不是完整包
            token.Buffer.AddRange(memory);
            do
            {
                int packageLen = token.Buffer.Data.ToInt32();
                if (packageLen > token.Buffer.Size - 4)
                {
                    break;
                }
                token.Proxy.DeBytes(token.Buffer.Data.Slice(0, packageLen + 4));
                await ReadConnectionPack(token).ConfigureAwait(false);

                token.Buffer.RemoveRange(0, packageLen + 4);
            } while (token.Buffer.Size > 4);
        }
        protected async Task CloseConnection(ITunnelConnection connection, object userToken)
        {
            CloseClientSocket(userToken as AsyncUserToken);
            await Task.CompletedTask;
        }
        private async Task ReadConnectionPack(AsyncUserToken token)
        {
            if (token.Proxy.Step == ProxyStep.Request)
            {
                await ConnectBind(token).ConfigureAwait(false);
            }
            else
            {
                await SendToSocket(token).ConfigureAwait(false);
            }
        }
        private async Task ConnectBind(AsyncUserToken token)
        {
            Socket socket = new Socket(token.Proxy.TargetEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.KeepAlive();
            await socket.ConnectAsync(token.Proxy.TargetEP);

            dic.TryAdd(new ConnectId(token.Proxy.ConnectId, token.Connection.GetHashCode()), socket);

            BindReceiveTarget(new AsyncUserToken
            {
                Connection = token.Connection,
                Socket = socket,
                Proxy = new ProxyInfo
                {
                    ConnectId = token.Proxy.ConnectId,
                    Step = ProxyStep.Forward
                }
            });
        }
        private async Task SendToSocket(AsyncUserToken token)
        {
            ConnectId connectId = new ConnectId(token.Proxy.ConnectId, token.Connection.GetHashCode());
            if (dic.TryGetValue(connectId, out Socket source))
            {
                try
                {
                    await source.SendAsync(token.Proxy.Data);
                }
                catch (Exception)
                {
                    CloseClientSocket(token);

                }
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
                                CloseClientSocket(token);
                                return;
                            }
                        }
                    }

                    if (token.Connection.Connected == false)
                    {
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

        private void CloseClientSocket(AsyncUserToken token)
        {
            if (token == null) return;
            if (token.Connection != null)
            {
                int code = token.Connection.GetHashCode();
                if (token.Connection.Connected == false)
                {
                    foreach (ConnectId item in dic.Keys.Where(c => c.hashCode == code).ToList())
                    {
                        dic.TryRemove(item, out _);
                    }
                }
                else
                {
                    dic.TryRemove(new ConnectId(token.Proxy.ConnectId, code), out _);
                }
            }
            token.Clear();
        }
        public void Stop()
        {
            CloseClientSocket(userToken);
            udpClient?.Close();
        }

    }

    public enum ProxyStep : byte
    {
        Request = 1,
        Forward = 2
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
    public sealed class ProxyInfo
    {
        public ulong ConnectId { get; set; }
        public ProxyStep Step { get; set; } = ProxyStep.Request;
        public IPEndPoint TargetEP { get; set; }

        public Memory<byte> Data { get; set; }

        public byte[] ToBytes(out int length)
        {
            int ipLength = TargetEP == null ? 0 : (TargetEP.AddressFamily == AddressFamily.InterNetwork ? 4 : 16) + 2;

            length = 4 + 8 + 1
                + 1 + ipLength
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

            bytes[index] = (byte)ipLength;
            index += 1;

            if (ipLength > 0)
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

        public void DeBytes(Memory<byte> memory)
        {
            int index = 4;
            Span<byte> span = memory.Span;

            ConnectId = memory.Slice(index).ToUInt64();
            index += 8;

            Step = (ProxyStep)span[index];
            index += 1;

            byte ipLength = span[index];
            index += 1;
            if (ipLength > 0)
            {
                IPAddress ip = new IPAddress(span.Slice(index, ipLength - 2));
                index += ipLength;
                ushort port = span.Slice(index - 2).ToUInt16();
                TargetEP = new IPEndPoint(ip, port);
            }
            Data = memory.Slice(index);
        }
    }

    public sealed class AsyncUserUdpToken
    {
        public UdpClient SourceSocket { get; set; }
        public Socket TargetSocket { get; set; }
        public ProxyInfo Proxy { get; set; }

        public void Clear()
        {
            SourceSocket?.Close();
            SourceSocket = null;
            GC.Collect();
        }
    }

    public sealed class AsyncUserToken
    {
        public Socket Socket { get; set; }
        public ITunnelConnection Connection { get; set; }

        public ProxyInfo Proxy { get; set; }

        public ReceiveDataBuffer Buffer { get; set; }

        public SocketAsyncEventArgs Saea { get; set; }

        public void Clear()
        {
            Socket?.SafeClose();
            Socket = null;

            Buffer?.Clear();

            Saea?.Dispose();

            GC.Collect();
        }
    }
}
