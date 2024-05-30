using common.libs;
using common.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace cmonitor.plugins.tunnel.server
{
    public sealed class TunnelBindServer
    {
        public Func<object, Socket, Task> OnTcpConnected { get; set; } = async (state, socket) => { await Task.CompletedTask; };
        public Func<object, UdpClient, Task> OnUdpConnected { get; set; } = async (state, udpClient) => { await Task.CompletedTask; };

        private ConcurrentDictionary<int, AsyncUserToken> acceptBinds = new ConcurrentDictionary<int, AsyncUserToken>();

        public void Bind(IPEndPoint local, object state)
        {
            try
            {
                IPAddress localIP = NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any;

                Socket socket = new Socket(localIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.IPv6Only(localIP.AddressFamily, false);
                socket.ReuseBind(new IPEndPoint(localIP, local.Port));
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
                acceptBinds.AddOrUpdate(local.Port, token, (a, b) => token);

                acceptEventArg.Completed += IO_Completed;
                StartAccept(acceptEventArg);


                token.UdpClient = new UdpClient(localIP.AddressFamily);
                token.UdpClient.Client.ReuseBind(new IPEndPoint(localIP, local.Port));
                //socketUdp.Client.EnableBroadcast = true;
                token.UdpClient.Client.WindowsUdpBug();
                IAsyncResult result = token.UdpClient.BeginReceive(ReceiveCallbackUdp, token);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        public void RemoveBind(int localPort, bool closeUdp)
        {
            if (acceptBinds.TryRemove(localPort, out AsyncUserToken saea))
            {
                CloseClientSocket(saea, closeUdp);
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
                token.Clear(true);
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
            AsyncUserToken token = result.AsyncState as AsyncUserToken;
            try
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                byte[] bytes = token.UdpClient.EndReceive(result, ref ep);
                string command = Encoding.UTF8.GetString(bytes);

                if (command == "snltty.end")
                {
                    OnUdpConnected(token.State, token.UdpClient);
                    return;
                }
                else if (command == "snltty.test")
                {
                    token.UdpClient.Send(bytes);
                }


                result = token.UdpClient.BeginReceive(ReceiveCallbackUdp, token);
            }
            catch (Exception)
            {
            }
        }

        private void CloseClientSocket(AsyncUserToken token, bool closeUdp)
        {
            if (token == null) return;

            Socket socket = token.SourceSocket;
            if (socket != null)
            {
                token.Clear(closeUdp);
                if (acceptBinds.TryRemove(token.LocalPort, out AsyncUserToken tk))
                {
                    CloseClientSocket(tk, closeUdp);
                }
            }
        }

        public sealed class AsyncUserToken
        {
            public Socket SourceSocket { get; set; }
            public SocketAsyncEventArgs Saea { get; set; }
            public object State { get; set; }
            public int LocalPort { get; set; }

            public UdpClient UdpClient { get; set; }

            public void Clear(bool closeUdp)
            {
                SourceSocket?.SafeClose();
                SourceSocket = null;

                if (closeUdp)
                    UdpClient?.Close();

                Saea?.Dispose();

                GC.Collect();
            }
        }
    }


}
