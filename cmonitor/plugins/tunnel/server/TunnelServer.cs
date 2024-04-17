using common.libs;
using common.libs.extends;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.tunnel.server
{
    public sealed class TunnelServer
    {
        private SocketAsyncEventArgs acceptEventArg;
        private Socket socket;
        private UdpClient socketUdp;

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


                socketUdp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
                socketUdp.Client.EnableBroadcast = true;
                socketUdp.Client.WindowsUdpBug();
                IAsyncResult result = socketUdp.BeginReceive(ReceiveCallbackUdp, null);
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
                WriteExternalIP(e);
                StartAccept(e);
            }
        }
        private async void WriteExternalIP(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            if (token.SourceSocket != null)
            {
                IPEndPoint ep = token.SourceSocket.RemoteEndPoint as IPEndPoint;

                byte[] bytes = ArrayPool<byte>.Shared.Rent(20);

                int index = 0;
                bytes[index] = (byte)ep.Address.AddressFamily;
                index++;
                ep.Address.TryWriteBytes(bytes.AsSpan(index), out int bytesWritten);
                index += bytesWritten;
                ((ushort)ep.Port).ToBytes(bytes.AsMemory(index));
                index += 2;

                await token.SourceSocket.SendAsync(bytes.AsMemory(0, index));

                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        private async void ReceiveCallbackUdp(IAsyncResult result)
        {
            try
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                byte[] _ = socketUdp.EndReceive(result, ref ep);


                byte[] bytes = ArrayPool<byte>.Shared.Rent(20);

                int index = 0;
                bytes[index] = (byte)ep.Address.AddressFamily;
                index++;
                ep.Address.TryWriteBytes(bytes.AsSpan(index), out int bytesWritten);
                index += bytesWritten;
                ((ushort)ep.Port).ToBytes(bytes.AsMemory(index));
                index += 2;

                await socketUdp.SendAsync(bytes.AsMemory(0, index), ep);

                ArrayPool<byte>.Shared.Return(bytes);

                result = socketUdp.BeginReceive(ReceiveCallbackUdp, null);
            }
            catch (Exception)
            {
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
        public void Stop()
        {
            CloseClientSocket(acceptEventArg);
        }
    }

    public sealed class AsyncUserToken
    {
        public Socket SourceSocket { get; set; }

        public void Clear()
        {
            SourceSocket?.SafeClose();
            SourceSocket = null;

            GC.Collect();
        }
    }
}
