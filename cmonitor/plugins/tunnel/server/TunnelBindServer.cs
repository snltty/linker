using common.libs;
using common.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.tunnel.server
{
    public sealed class TunnelBindServer
    {
        private UdpClient socketUdp;

        public Action<object, Socket> OnTcpConnected { get; set; } = (state, socket) => { };
        public Action<object, UdpClient> OnUdpConnected { get; set; } = (state, udpClient) => { };
        public Action<object> OnDisConnected { get; set; } = (state) => { };

        private ConcurrentDictionary<int, SocketAsyncEventArgs> acceptBinds = new ConcurrentDictionary<int, SocketAsyncEventArgs>();

        public void Bind(IPEndPoint local, object state)
        {
            try
            {
                Socket socket = new Socket(local.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.IPv6Only(local.AddressFamily, false);
                socket.ReuseBind(new IPEndPoint(IPAddress.Any, local.Port));
                socket.Listen(int.MaxValue);

                AsyncUserToken token = new AsyncUserToken
                {
                    SourceSocket = socket,
                    State = state,
                    LocalPort = local.Port
                };
                SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs
                {
                    UserToken = token,
                    SocketFlags = SocketFlags.None,
                };
                token.Saea = acceptEventArg;
                acceptBinds.AddOrUpdate(local.Port, acceptEventArg, (a, b) => acceptEventArg);

                acceptEventArg.Completed += IO_Completed;
                StartAccept(acceptEventArg);


                socketUdp = new UdpClient();
                socketUdp.Client.ReuseBind(new IPEndPoint(IPAddress.Any, local.Port));
                socketUdp.Client.EnableBroadcast = true;
                socketUdp.Client.WindowsUdpBug();
                IAsyncResult result = socketUdp.BeginReceive(ReceiveCallbackUdp, state);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        public void RemoveBind(int localPort)
        {
            if (acceptBinds.TryRemove(localPort, out SocketAsyncEventArgs saea))
            {
                CloseClientSocket(saea);
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
                if (e.AcceptSocket.RemoteEndPoint != null)
                {

                    AsyncUserToken token = (AsyncUserToken)e.UserToken;
                    OnTcpConnected(token.State, e.AcceptSocket);
                }
            }
            StartAccept(e);
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
                OnData = dataCallback,
                LocalPort = (socket.LocalEndPoint as IPEndPoint).Port,
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
            if (e == null || e.UserToken == null) return;

            AsyncUserToken token = e.UserToken as AsyncUserToken;
            Socket socket = token.SourceSocket;
            if (socket != null)
            {
                token.Clear();
                e.Dispose();
                if (acceptBinds.TryRemove(token.LocalPort, out SocketAsyncEventArgs saea1))
                {
                    CloseClientSocket(saea1);
                }
                OnDisConnected(token.State);
            }
        }


        public delegate Task OnTunnelData(AsyncUserToken token, Memory<byte> data);
        public delegate Task OnTunnelUdpData(AsyncUserUdpToken token, IPEndPoint remote, Memory<byte> data);
        public sealed class AsyncUserToken
        {
            public Socket SourceSocket { get; set; }
            public SocketAsyncEventArgs Saea { get; set; }
            public object State { get; set; }

            public OnTunnelData OnData { get; set; }

            public int LocalPort { get; set; }

            public void Clear()
            {
                SourceSocket?.SafeClose();
                SourceSocket = null;

                GC.Collect();
            }
        }
        public sealed class AsyncUserUdpToken
        {
            public UdpClient SourceSocket { get; set; }
            public object State { get; set; }
            public OnTunnelUdpData OnData { get; set; }


            public void Clear()
            {
                SourceSocket?.Close();
                SourceSocket = null;

                GC.Collect();
            }
        }
    }


}
