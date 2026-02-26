using linker.libs;
using linker.libs.extends;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.listen
{
    public sealed class TcpServer
    {
        private Socket socket;
        private Socket socketUdp;
        private CancellationTokenSource cancellationTokenSource;

        private readonly ResolverTransfer resolverTransfer;
        private readonly CountryTransfer countryTransfer;
        public TcpServer(ResolverTransfer resolverTransfer, CountryTransfer countryTransfer)
        {
            this.resolverTransfer = resolverTransfer;
            this.countryTransfer = countryTransfer;
            cancellationTokenSource = new CancellationTokenSource();

        }

        public void Start(int port)
        {
            if (port <= 0) return;
            if (socket == null)
            {
                socket = BindAccept(port);
            }
        }

        private async Task BindUdp(int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.IPv6Any, port);
            socketUdp = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socketUdp.Bind(localEndPoint);
            socketUdp.IPv6Only(socketUdp.AddressFamily, false);
            socketUdp.WindowsUdpBug();
            IPEndPoint endPoint = new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort);
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(65535);
            while (true)
            {
                try
                {
                    SocketReceiveFromResult result = await socketUdp.ReceiveFromAsync(buffer.Memory, SocketFlags.None, endPoint).ConfigureAwait(false);
                    if (result.ReceivedBytes == 0)
                    {
                        LoggerHelper.Instance.Error($"udp server recv 0");
                        continue;
                    }
                    IPEndPoint ep = result.RemoteEndPoint as IPEndPoint;
                    try
                    {
                        if (countryTransfer.Test(buffer.Memory.Span[0], ep.Address) == false)
                        {
                            continue;
                        }

                        await resolverTransfer.BeginReceive(socketUdp, ep, buffer.Memory.Slice(0, result.ReceivedBytes)).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Instance.Error($"udp server recv {ex}");
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Error($"udp server recv {ex}");
                    break;
                }
            }
        }

        private Socket BindAccept(int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.IPv6Any, port);
            Socket socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.IPv6Only(localEndPoint.AddressFamily, false);
            socket.Bind(localEndPoint);
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
                BeginReceive(e.AcceptSocket).ConfigureAwait(false);
                StartAccept(e);
            }
        }
        private async Task BeginReceive(Socket socket)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(32);
            using CancellationTokenSource cts = new CancellationTokenSource(5000);
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return;
                }

                int length = await socket.ReceiveAsync(buffer.AsMemory(0, 1), SocketFlags.None, cts.Token).ConfigureAwait(false);
                byte type = buffer[0];
                if (countryTransfer.Test(type, (socket.RemoteEndPoint as IPEndPoint).Address) == false)
                {
                    cts.Cancel();
                    socket.SafeClose();
                    return;
                }
                _ = resolverTransfer.BeginReceive(type, socket);
            }
            catch (Exception ex)
            {
                cts.Cancel();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);

                socket.SafeClose();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
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
