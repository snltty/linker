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

        public sealed class AsyncUserToken
        {
            public Socket SourceSocket { get; set; }
            public object State { get; set; }

            public void Clear()
            {
                SourceSocket?.SafeClose();
                SourceSocket = null;

                GC.Collect();
            }
        }
    }


}
