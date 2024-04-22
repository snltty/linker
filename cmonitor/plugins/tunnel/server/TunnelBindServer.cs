using common.libs;
using common.libs.extends;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.tunnel.server
{
    public sealed class TunnelBindServer
    {
        private SocketAsyncEventArgs acceptEventArg;
        private Socket socket;
        private UdpClient socketUdp;

        public Action<object, Socket> OnTcpConnected { get; set; } = (state, socket) => { };
        public Action<object, UdpClient> OnUdpConnected { get; set; } = (state, udpClient) => { };

        public void Bind(IPEndPoint local, object state)
        {
            try
            {
                socket = new Socket(local.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.IPv6Only(local.AddressFamily, false);
                socket.ReuseBind(local);
                socket.Listen(int.MaxValue);

                acceptEventArg = new SocketAsyncEventArgs
                {
                    UserToken = new AsyncUserToken
                    {
                        SourceSocket = socket,
                        State = state
                    },
                    SocketFlags = SocketFlags.None,
                };

                acceptEventArg.Completed += IO_Completed;
                StartAccept(acceptEventArg);


                socketUdp = new UdpClient();
                socketUdp.Client.ReuseBind(local);
                socketUdp.Client.EnableBroadcast = true;
                socketUdp.Client.WindowsUdpBug();
                IAsyncResult result = socketUdp.BeginReceive(ReceiveCallbackUdp, state);
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
                default:
                    break;
            }
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.AcceptSocket != null)
            {
                AsyncUserToken token = e.UserToken as AsyncUserToken;
                OnTcpConnected(token.State, e.AcceptSocket);
                //BindReceive(e);
                StartAccept(e);
            }
        }
        private void ReceiveCallbackUdp(IAsyncResult result)
        {
            try
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                byte[] _ = socketUdp.EndReceive(result, ref ep);

                OnUdpConnected(result.AsyncState, socketUdp);
            }
            catch (Exception)
            {
            }
        }

        public void BindReceive(Socket socket, object state, OnTunnelData dataCallback)
        {
            if (socket == null || socket.RemoteEndPoint == null)
            {
                return;
            }

            socket.KeepAlive();
            AsyncUserToken userToken = new AsyncUserToken
            {
                SourceSocket = socket,
                State = state,
                OnData = dataCallback
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
        private void BindReceive(SocketAsyncEventArgs e)
        {
            try
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                var socket = e.AcceptSocket;

                BindReceive(socket, token.State, token.OnData);
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

                    await token.OnData(token, e.Buffer.AsMemory(0, length));
                    if (token.SourceSocket.Available > 0)
                    {
                        while (token.SourceSocket.Available > 0)
                        {
                            length = token.SourceSocket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                await token.OnData(token, e.Buffer.AsMemory(0, length));
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
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            if (e == null) return;
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            if (token.SourceSocket != null)
            {
                token.Clear();
                e.Dispose();
            }
        }


        public delegate Task OnTunnelData(AsyncUserToken token, Memory<byte> data);
        public sealed class AsyncUserToken
        {
            public Socket SourceSocket { get; set; }
            public object State { get; set; }
            public OnTunnelData OnData { get; set; }


            public void Clear()
            {
                SourceSocket?.SafeClose();
                SourceSocket = null;

                GC.Collect();
            }
        }
    }


}
