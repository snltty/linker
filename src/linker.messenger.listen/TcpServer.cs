using linker.libs;
using linker.libs.extends;
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

        public void Start(int port, bool ipv6 = false)
        {
            if (port <= 0)
            {
                return;
            }
            if (socket == null)
            {
                IPEndPoint localEndPoint = ipv6 ? new IPEndPoint(IPAddress.IPv6Any, port) : new IPEndPoint(IPAddress.Any, port);
                _ = BindTcp(localEndPoint);
                _ = BindUdp(localEndPoint);
            }
        }
        private async Task BindTcp(IPEndPoint localEndPoint)
        {
            socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.IPv6Only(localEndPoint.AddressFamily, false);
            socket.Bind(localEndPoint);
            socket.Listen(int.MaxValue);

            while (true)
            {
                try
                {
                    var acceptTask = await socket.AcceptAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                    if (acceptTask != null && acceptTask.RemoteEndPoint != null)
                    {
                        _ = BeginReceive(acceptTask).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Debug($"tcp server accept {ex}");
                    }
                }
            }
        }
        private async Task BindUdp(IPEndPoint localEndPoint)
        {
            socketUdp = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socketUdp.IPv6Only(localEndPoint.AddressFamily, false);
            socketUdp.Bind(localEndPoint);
            socketUdp.WindowsUdpBug();
            IPEndPoint endPoint = localEndPoint.AddressFamily == AddressFamily.InterNetworkV6 ? new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort) : new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
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
                    IPEndPoint ep = (result.RemoteEndPoint as IPEndPoint).MapToIPv4();
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

        private async Task BeginReceive(Socket socket)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            using CancellationTokenSource cts = new CancellationTokenSource(5000);
            try
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Debug($"tcp server connect from {socket.RemoteEndPoint}");
                }
                int length = await socket.ReceiveAsync(buffer.AsMemory(0, 1), SocketFlags.None, cts.Token).ConfigureAwait(false);
                if (length == 0)
                {
                    cts.Cancel();
                    socket.SafeClose();
                    return;
                }

                byte type = buffer[0];
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Debug($"tcp server got {type} from {socket.RemoteEndPoint}");
                }
                if (countryTransfer.Test(type, (socket.RemoteEndPoint as IPEndPoint).Address.MapToIPv4()) == false)
                {
                    cts.Cancel();
                    socket.SafeClose();
                    return;
                }
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Debug($"tcp server begin recv {type} with {socket.RemoteEndPoint}");
                }
                _ = resolverTransfer.BeginReceive(type, socket).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                cts.Cancel();
                socket.SafeClose();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
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
            socketUdp?.SafeClose();
            socketUdp = null;
        }
    }
}
