using cmonitor.config;
using common.libs;
using common.libs.extends;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace cmonitor.server
{
    public sealed class TcpServer
    {
        private int bufferSize = 8 * 1024;
        private Socket socket;
        private UdpClient socketUdp;
        private CancellationTokenSource cancellationTokenSource;
        private Memory<byte> relayFLagCData = Encoding.UTF8.GetBytes("snltty.relay");

        public Func<IConnection, Task> OnPacket { get; set; } = async (connection) => { await Task.CompletedTask; };
        public Action<int> OnDisconnected { get; set; }

        public TcpServer()
        {
        }
        public void Start(int port)
        {
            if (socket == null)
            {
                cancellationTokenSource = new CancellationTokenSource();
                socket = BindAccept(port);
            }
        }

        private Socket BindAccept(int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any, port);
            Socket socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.IPv6Only(localEndPoint.AddressFamily, false);
            socket.ReuseBind(localEndPoint);
            socket.Listen(int.MaxValue);

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs
            {
                UserToken = new AsyncUserToken
                {
                    Socket = socket
                },
                SocketFlags = SocketFlags.None,
            };
            acceptEventArg.Completed += IO_Completed;
            StartAccept(acceptEventArg);

            socketUdp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            //socketUdp.JoinMulticastGroup(config.BroadcastIP);
            socketUdp.Client.EnableBroadcast = true;
            socketUdp.Client.WindowsUdpBug();
            IAsyncResult result = socketUdp.BeginReceive(ReceiveCallbackUdp, null);


            return socket;

        }
        private async void ReceiveCallbackUdp(IAsyncResult result)
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                byte[] bytes = socketUdp.EndReceive(result, ref endPoint);
                try
                {
                    await socketUdp.SendAsync(endPoint.ToString().ToBytes(), endPoint);
                }
                catch (Exception)
                {
                }

                result = socketUdp.BeginReceive(ReceiveCallbackUdp, null);
            }
            catch (Exception)
            {
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
                BindReceive(e.AcceptSocket);
                StartAccept(e);
            }
        }

        public IConnection BindReceive(Socket socket)
        {
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return null;
                }

                socket.KeepAlive();
                AsyncUserToken userToken = new AsyncUserToken
                {
                    Socket = socket,
                    Connection = CreateConnection(socket)
                };

                SocketAsyncEventArgs saea = new SocketAsyncEventArgs
                {
                    UserToken = userToken,
                    SocketFlags = SocketFlags.None,
                };
                userToken.PoolBuffer = new byte[bufferSize];
                saea.SetBuffer(userToken.PoolBuffer, 0, bufferSize);
                saea.Completed += IO_Completed;
                if (socket.ReceiveAsync(saea) == false)
                {
                    ProcessReceive(saea);
                }
                return userToken.Connection;
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }
            return null;
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

                    bool res = await ReadPacket(token, e.Buffer, offset, length);
                    if (res == false)
                    {
                        return;
                    }

                    if (token.Socket.Available > 0)
                    {
                        while (token.Socket.Available > 0)
                        {
                            length = token.Socket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                res = await ReadPacket(token, e.Buffer, 0, length);
                                if (res == false)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                CloseClientSocket(e);
                                return;
                            }
                        }
                    }

                    if (token.Socket.Connected == false)
                    {
                        CloseClientSocket(e);
                        return;
                    }
                    if (token.Socket.ReceiveAsync(e) == false)
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
        private async Task<bool> ReadPacket(AsyncUserToken token, byte[] data, int offset, int length)
        {
            if (token.Connection.TcpTargetSocket != null)
            {
                if (token.DataBuffer.Size > 0)
                {
                    await token.Connection.TcpTargetSocket.SendAsync(token.DataBuffer.Data.Slice(0, token.DataBuffer.Size), SocketFlags.None);
                    token.DataBuffer.Clear();
                }
                await token.Connection.TcpTargetSocket.SendAsync(data.AsMemory(offset, length), SocketFlags.None);
                return true;
            }
            else if (length == relayFLagCData.Length && data.AsSpan(offset, relayFLagCData.Length).SequenceEqual(relayFLagCData.Span))
            {
                return false;
            }
            else
            {
                //是一个完整的包
                if (token.DataBuffer.Size == 0 && length > 4)
                {
                    Memory<byte> memory = data.AsMemory(offset, length);
                    int packageLen = memory.Span.ToInt32();
                    if (packageLen == length - 4)
                    {
                        token.Connection.ReceiveData = data.AsMemory(offset, packageLen + 4);
                        await OnPacket(token.Connection);
                        return true;
                    }
                }

                //不是完整包
                token.DataBuffer.AddRange(data, offset, length);
                do
                {
                    int packageLen = token.DataBuffer.Data.Span.ToInt32();
                    if (packageLen > token.DataBuffer.Size - 4)
                    {
                        break;
                    }
                    token.Connection.ReceiveData = token.DataBuffer.Data.Slice(0, packageLen + 4);
                    await OnPacket(token.Connection);

                    token.DataBuffer.RemoveRange(0, packageLen + 4);
                } while (token.DataBuffer.Size > 4);
            }
            return true;
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            if (token.Socket != null)
            {
                token.Clear();
                e.Dispose();
            }
            if (token.Socket != null)
                OnDisconnected?.Invoke(token.Socket.GetHashCode());
        }
        public IConnection CreateConnection(Socket socket)
        {
            return new TcpConnection(socket)
            {
                ReceiveRequestWrap = new MessageRequestWrap(),
                ReceiveResponseWrap = new MessageResponseWrap()
            };
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            socket?.SafeClose();
            socket = null;
        }
        public void Disponse()
        {
            Stop();
            OnPacket = null;
        }
    }


    public sealed class AsyncUserToken
    {
        public IConnection Connection { get; set; }
        public Socket Socket { get; set; }
        public ReceiveDataBuffer DataBuffer { get; set; } = new ReceiveDataBuffer();
        public byte[] PoolBuffer { get; set; }

        public void Clear()
        {
            Connection?.Disponse();
            Socket = null;



            PoolBuffer = Helper.EmptyArray;

            DataBuffer.Clear(true);

            GC.Collect();
            // GC.SuppressFinalize(this);
        }
    }
}
