using linker.libs.extends;
using linker.plugins.messenger;
using System.Net;
using System.Net.Sockets;

namespace linker.plugins.server
{
    public sealed class TcpServer
    {
        private Socket socket;
        private Socket socketUdp;
        private CancellationTokenSource cancellationTokenSource;
        private readonly MessengerResolver messengerResolver;

        public TcpServer(MessengerResolver messengerResolver)
        {
            this.messengerResolver = messengerResolver;
            cancellationTokenSource = new CancellationTokenSource();

        }

        public void Start(int port)
        {
            if (socket == null)
            {
                socket = BindAccept(port);
            }
        }

        private async Task BindUdp(int port)
        {
            socketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socketUdp.Bind(new IPEndPoint(IPAddress.Any, port));
            socketUdp.WindowsUdpBug();
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            byte[] buffer = new byte[1024];
            byte[] sendData = new byte[20];
            while (true)
            {
                try
                {
                    SocketReceiveFromResult result = await socketUdp.ReceiveFromAsync(buffer, SocketFlags.None, endPoint).ConfigureAwait(false);
                    IPEndPoint ep = result.RemoteEndPoint as IPEndPoint;
                    try
                    {
                        Memory<byte> memory = messengerResolver.BuildSendData(sendData, ep);

                        await socketUdp.SendToAsync(memory, ep).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        private Socket BindAccept(int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
            Socket socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.IPv6Only(localEndPoint.AddressFamily, false);
            socket.ReuseBind(localEndPoint);
            socket.Listen(int.MaxValue);

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs
            {
                UserToken = socket,
                SocketFlags = SocketFlags.None,
            };
            acceptEventArg.Completed += IO_Completed;
            StartAccept(acceptEventArg);

            _ = BindUdp(port);
            return socket;
        }
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            acceptEventArg.AcceptSocket = null;
            Socket token = (Socket)acceptEventArg.UserToken;
            try
            {
                if (token.AcceptAsync(acceptEventArg) == false)
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception)
            {
                token?.SafeClose();
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
                _ = messengerResolver.BeginReceiveServer(e.AcceptSocket);
                StartAccept(e);
            }
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
        }
    }
}
