using common.libs;
using common.libs.extends;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.viewer.proxy
{
    public class ViewerProxy
    {
        private SocketAsyncEventArgs acceptEventArg;
        private Socket socket;
        private NumberSpace ns = new NumberSpace();

        public IPEndPoint LocalEndpoint => socket?.LocalEndPoint as IPEndPoint ?? new IPEndPoint(IPAddress.Any, 0);

        public ViewerProxy()
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

                acceptEventArg = new SocketAsyncEventArgs
                {
                    UserToken = new AsyncUserToken
                    {
                        SourceSocket = socket
                    },
                    SocketFlags = SocketFlags.None,
                };

                acceptEventArg.Completed += IO_Completed;
                StartAccept(acceptEventArg);

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
                if (token.SourceSocket.AcceptAsync(acceptEventArg) == false)
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
                    SourceSocket = socket,
                    Proxy = new ProxyInfo { Data = Helper.EmptyArray, Step = ProxyStep.Request, ConnectId = ns.Increment() }
                };

                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = userToken,
                    SocketFlags = SocketFlags.None,
                };
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
            try
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;

                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int offset = e.Offset;
                    int length = e.BytesTransferred;
                    await ReadPacket(e, token, e.Buffer.AsMemory(offset, length));
                    if (token.SourceSocket.Available > 0)
                    {
                        while (token.SourceSocket.Available > 0)
                        {
                            length = token.SourceSocket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                await ReadPacket(e, token, e.Buffer.AsMemory(0, length));
                            }
                            else
                            {
                                CloseClientSocket(e);
                                return;
                            }
                        }
                    }

                    if (token.SourceSocket.Connected == false)
                    {
                        CloseClientSocket(e);
                        return;
                    }

                    if (token.SourceSocket.ReceiveAsync(e) == false)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);

                CloseClientSocket(e);
            }
        }

        private async Task ReadPacket(SocketAsyncEventArgs e, AsyncUserToken token, Memory<byte> data)
        {
            if (token.Proxy.Step == ProxyStep.Request)
            {
                await Connect(token, token.Proxy);
                if (token.TargetSocket != null)
                {
                    //发送连接请求包
                    await SendToTarget(e, token).ConfigureAwait(false);

                    token.Proxy.Step = ProxyStep.Forward;
                    token.Proxy.TargetEP = null;

                    //发送后续数据包
                    token.Proxy.Data = data;
                    await SendToTarget(e, token).ConfigureAwait(false);

                    //绑定
                    dic.TryAdd(new ConnectId(token.Proxy.ConnectId, token.TargetSocket.GetHashCode()), token.SourceSocket);
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            else
            {
                token.Proxy.Data = data;
                await SendToTarget(e, token).ConfigureAwait(false);
            }
        }
        private async Task SendToTarget(SocketAsyncEventArgs e, AsyncUserToken token)
        {
            byte[] connectData = token.Proxy.ToBytes(out int length);
            try
            {
                await token.TargetSocket.SendAsync(connectData.AsMemory(0, length), SocketFlags.None);
            }
            catch (Exception)
            {
                CloseClientSocket(e);
            }
            finally
            {
                token.Proxy.Return(connectData);
            }
        }

        protected virtual async Task Connect(AsyncUserToken token, ProxyInfo proxyInfo)
        {
            await Task.CompletedTask;
        }

        protected bool BindReceiveTarget(Socket targetSocket, Socket sourceSocket)
        {
            try
            {
                BindReceiveTarget(new AsyncUserToken
                {
                    TargetSocket = targetSocket,
                    SourceSocket = sourceSocket,
                    Buffer = new ReceiveDataBuffer(),
                    Proxy = new ProxyInfo { Direction = ProxyDirection.UnPack }
                });

                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            return false;
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
                if (userToken.TargetSocket.ReceiveAsync(readEventArgs) == false)
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
            try
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;

                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int offset = e.Offset;
                    int length = e.BytesTransferred;

                    await ReadPacketTarget(e, token, e.Buffer.AsMemory(offset, length)).ConfigureAwait(false);

                    if (token.TargetSocket.Available > 0)
                    {
                        while (token.TargetSocket.Available > 0)
                        {
                            length = token.TargetSocket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                await ReadPacketTarget(e, token, e.Buffer.AsMemory(0, length)).ConfigureAwait(false);
                            }
                            else
                            {
                                CloseClientSocket(e);
                                return;
                            }
                        }
                    }

                    if (token.TargetSocket.Connected == false)
                    {
                        CloseClientSocket(e);
                        return;
                    }

                    if (token.TargetSocket.ReceiveAsync(e) == false)
                    {
                        ProcessReceiveTarget(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);

                CloseClientSocket(e);
            }
        }

        private readonly ConcurrentDictionary<ConnectId, Socket> dic = new ConcurrentDictionary<ConnectId, Socket>();
        private async Task ReadPacketTarget(SocketAsyncEventArgs e, AsyncUserToken token, Memory<byte> data)
        {
            //A 到 B 
            if (token.Proxy.Direction == ProxyDirection.UnPack)
            {
                //是一个完整的包
                if (token.Buffer.Size == 0 && data.Length > 4)
                {
                    int packageLen = data.ToInt32();
                    if (packageLen == data.Length - 4)
                    {
                        token.Proxy.DeBytes(data.Slice(0, packageLen + 4));
                        await ReadPacketTarget(e, token).ConfigureAwait(false);
                        return;
                    }
                }

                //不是完整包
                token.Buffer.AddRange(data);
                do
                {
                    int packageLen = token.Buffer.Data.ToInt32();
                    if (packageLen > token.Buffer.Size - 4)
                    {
                        break;
                    }
                    token.Proxy.DeBytes(token.Buffer.Data.Slice(0, packageLen + 4));
                    await ReadPacketTarget(e, token).ConfigureAwait(false);

                    token.Buffer.RemoveRange(0, packageLen + 4);
                } while (token.Buffer.Size > 4);
            }
            else
            {
                token.Proxy.Data = data;
                await SendToSource(e, token).ConfigureAwait(false);
            }
        }
        private async Task ReadPacketTarget(SocketAsyncEventArgs e, AsyncUserToken token)
        {
            if (token.Proxy.Step == ProxyStep.Request)
            {
                await ConnectBind(e, token).ConfigureAwait(false);
            }
            else
            {
                await SendToSource(e, token).ConfigureAwait(false);
            }
        }
        private async Task ConnectBind(SocketAsyncEventArgs e, AsyncUserToken token)
        {
            Socket socket = new Socket(token.Proxy.TargetEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.KeepAlive();
            await socket.ConnectAsync(token.Proxy.TargetEP);

            dic.TryAdd(new ConnectId(token.Proxy.ConnectId, token.TargetSocket.GetHashCode()), socket);

            BindReceiveTarget(new AsyncUserToken
            {
                TargetSocket = socket,
                SourceSocket = token.TargetSocket,
                Proxy = new ProxyInfo
                {
                    Direction = ProxyDirection.Pack,
                    ConnectId = token.Proxy.ConnectId,
                    Step = ProxyStep.Forward
                }
            });
        }
        private async Task SendToSource(SocketAsyncEventArgs e, AsyncUserToken token)
        {
            if(token.Proxy.Direction == ProxyDirection.UnPack)
            {
                ConnectId connectId = new ConnectId(token.Proxy.ConnectId, token.TargetSocket.GetHashCode());
                if (dic.TryGetValue(connectId, out Socket source))
                {
                    try
                    {
                        await source.SendAsync(token.Proxy.Data);
                    }
                    catch (Exception)
                    {
                        CloseClientSocket(e);

                    }
                }
            }
            else
            {
                byte[] connectData = token.Proxy.ToBytes(out int length);
                try
                {
                    await token.SourceSocket.SendAsync(connectData.AsMemory(0, length), SocketFlags.None);
                }
                catch (Exception)
                {
                    CloseClientSocket(e);
                }
                finally
                {
                    token.Proxy.Return(connectData);
                }
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            if (e == null) return;
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            if (token.TargetSocket != null)
            {
                int code = token.TargetSocket.GetHashCode();
                if (token.TargetSocket.Connected == false)
                {
                    foreach (ConnectId item in dic.Keys.Where(c => c.socket == code).ToList())
                    {
                        dic.TryRemove(item, out _);
                    }
                }
                else
                {
                    dic.TryRemove(new ConnectId(token.Proxy.ConnectId, code), out _);
                }
            }
            if (token.SourceSocket != null)
            {
                token.Clear();
                e.Dispose();
            }
        }
        public void Stop()
        {
            CloseClientSocket(acceptEventArg);
        }

    }

    public sealed class ProxyInfo
    {
        public ulong ConnectId { get; set; }

        public ProxyStep Step { get; set; } = ProxyStep.Request;

        public ProxyDirection Direction { get; set; } = ProxyDirection.Pack;

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

    public sealed class AsyncUserToken
    {
        public Socket SourceSocket { get; set; }
        public Socket TargetSocket { get; set; }

        public ProxyInfo Proxy { get; set; }

        public ReceiveDataBuffer Buffer { get; set; }

        public void Clear()
        {
            SourceSocket?.SafeClose();
            SourceSocket = null;

            Buffer?.Clear();
            //TargetSocket?.SafeClose();
            //TargetSocket = null;

            GC.Collect();
        }
    }

    public enum ProxyStep : byte
    {
        Request = 1,
        Forward = 2
    }

    public enum ProxyDirection
    {
        Pack = 0,
        UnPack = 1,
    }

    public record struct ConnectId
    {
        public ulong connectId;
        public int socket;

        public ConnectId(ulong connectId, int socket)
        {
            this.connectId = connectId;
            this.socket = socket;
        }
    }
}
