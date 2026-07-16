
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using linker.tunnel.wanport;
using System.Buffers;

namespace linker.tunnel.transport
{
    public sealed class TransportUdpTest : ITunnelTransport
    {
        public string Name => "UdpTest";
        public string Label => "UDP、测试";
        public TunnelProtocolType ProtocolType => TunnelProtocolType.Udp;
        public TunnelWanPortProtocolType AllowWanPortProtocolType => TunnelWanPortProtocolType.Udp;
        public TunnelType TunnelType => TunnelType.P2P;
        public bool Reverse => true;

        public bool DisableReverse => false;

        public bool SSL => true;

        public bool DisableSSL => false;

        public byte Order => 3;

        public bool EnableAddr => true;

        public Action<ITunnelConnection, TunnelTransportInfo> OnConnected { get; set; } = (state, info) => { };


        private readonly byte[] authBytes = Encoding.UTF8.GetBytes($"GET /snltty/tcp/index.html HTTP/1.1\r\nHost: www.snltty.com\r\nConnection: keep-alive\r\nTransfer-Encoding: chunked\r\nUser-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36\r\nAccept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\nCookie: {Helper.GlobalString}.udp.ttl1\r\n\r\n");
        private readonly byte[] endBytes = Encoding.UTF8.GetBytes($"HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: keep-alive\r\nContent-Type: text/html\r\nCookie: {Helper.GlobalString}.udp.end1\r\n\r\nOK");

        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;
        public TransportUdpTest(ITunnelMessengerAdapter tunnelMessengerAdapter)
        {
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }
        public void SetSSL(X509Certificate certificate)
        {
        }

        public async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo).ConfigureAwait(false) == false)
            {
                return null;
            }
            await Task.Delay(50).ConfigureAwait(false);
            ITunnelConnection connection = await ConnectForward(tunnelTransportInfo, TunnelMode.Client).ConfigureAwait(false);
            if (connection != null)
            {
                await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                return connection;
            }

            await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
            return null;
        }
        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            ITunnelConnection connection = await ConnectForward(tunnelTransportInfo, TunnelMode.Server).ConfigureAwait(false);
            if (connection != null)
            {
                OnConnected(connection, tunnelTransportInfo);
                await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
            }
            else
            {
                await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
            }
        }

        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
        }
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {

        }

        private async Task<ITunnelConnection> ConnectForward(TunnelTransportInfo info, TunnelMode mode)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Warning($"{Name} connect to {info.Remote.MachineId}->{info.Remote.MachineName} {string.Join("\r\n", info.RemoteEndPoints.FirstOrDefault())}");
            }

            List<Socket> sockets = CreateSockets(info.Local.Local.Port);

            using CancellationTokenSource cts = new CancellationTokenSource(15000);
            _ = SendData(sockets, info.RemoteEndPoints, cts.Token);
            (Socket targetSocket, IPEndPoint remote) = await WaitRcv(sockets, cts).ConfigureAwait(false);
            if (targetSocket != null)
            {
                await ClearData(targetSocket).ConfigureAwait(false);
                ISymmetricCryptoGcm crypto = mode == TunnelMode.Client ? CryptoFactory.CreateSymmetricGcm(info.Remote.MachineId) : CryptoFactory.CreateSymmetricGcm(info.Local.MachineId);
                return new TunnelConnectionUdp
                {
                    UdpClient = targetSocket,
                    RemoteMachineId = info.Remote.MachineId,
                    RemoteMachineName = info.Remote.MachineName,
                    Direction = info.Direction,
                    ProtocolType = TunnelProtocolType.Udp,
                    Type = TunnelType,
                    Mode = mode,
                    TransactionId = info.TransactionId,
                    Configure = info.Configure,
                    TransportName = info.TransportName,
                    IPEndPoint = remote.MapToIPv4(),
                    Label = string.Empty,
                    Receive = true,
                    SSL = info.SSL,
                    Crypto = crypto
                };
            }
            return null;
        }

        private List<Socket> CreateSockets(int localPort)
        {
            List<Socket> sockets = new List<Socket>(26);

            Socket socket = new(AddressFamily.InterNetworkV6, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            socket.IPv6Only(AddressFamily.InterNetworkV6, false);
            socket.WindowsUdpBug();
            socket.ReuseBind(new IPEndPoint(IPAddress.IPv6Any, localPort));
            sockets.Add(socket);
            for (int i = 0; i < 25; i++)
            {
                socket = new(AddressFamily.InterNetworkV6, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                socket.IPv6Only(AddressFamily.InterNetworkV6, false);
                socket.WindowsUdpBug();
                socket.ReuseBind(new IPEndPoint(IPAddress.IPv6Any, 0));
                sockets.Add(socket);
            }
            return sockets;
        }
        private async Task SendData(List<Socket> sockets, List<IPEndPoint> remoteEndPoints, CancellationToken token)
        {
            //先IPV4，后IPV6，不能一起试，会导致异常

            for (int i = 0; i < 50 && token.IsCancellationRequested == false; i++)
            {
                foreach (var socket in sockets)
                {
                    int index = 0;
                    foreach (var item in remoteEndPoints.Where(c => c.AddressFamily == AddressFamily.InterNetwork))
                    {
                        socket.SendTo(authBytes, item);
                        if(++index % 15 == 0)
                        {
                            await Task.Delay(100, token).ConfigureAwait(false);
                        }
                    }
                }
               
                await Task.Delay(100, token).ConfigureAwait(false);
            }
            for (int i = 0; i < 50 && token.IsCancellationRequested == false; i++)
            {
                foreach (var socket in sockets)
                {
                    int index = 0;
                    foreach (var item in remoteEndPoints.Where(c => c.AddressFamily == AddressFamily.InterNetworkV6))
                    {
                        socket.SendTo(authBytes, item);
                        if (++index % 15 == 0)
                        {
                            await Task.Delay(100, token).ConfigureAwait(false);
                        }
                    }
                }
                await Task.Delay(100, token).ConfigureAwait(false);
            }
        }
        private async Task ClearData(Socket socket)
        {
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(1024);
            IPEndPoint tempEP = new IPEndPoint(IPAddress.IPv6Any, 0);
            while (true)
            {
                using CancellationTokenSource cts1 = new CancellationTokenSource(1000);
                try
                {
                    SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer.Memory, tempEP, cts1.Token).ConfigureAwait(false);
                    if (buffer.Memory.Span.Slice(0, result.ReceivedBytes).SequenceEqual(authBytes))
                    {
                        socket.SendTo(endBytes, result.RemoteEndPoint);
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }
        }
        private async Task<(Socket socket, IPEndPoint remote)> WaitRcv(List<Socket> sockets, CancellationTokenSource cts)
        {
            IPEndPoint tempEP = new IPEndPoint(IPAddress.IPv6Any, 0);
            List<Task<(Socket socket, IPEndPoint remote)>> tasks = sockets.Select(c => WaitRcv(c, cts)).ToList();
            await Task.WhenAll(tasks).ConfigureAwait(false);

            var success = tasks.Where(c => c.IsCompletedSuccessfully && c.Result.socket != null).ToList();
            (Socket socket, IPEndPoint remote) result = (null, null);

            if (success.Count > 0)
            {
                result = success[0].Result;
            }
            return result;
        }
        private async Task<(Socket socket, IPEndPoint remote)> WaitRcv(Socket socket, CancellationTokenSource cts)
        {
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(1024);
            IPEndPoint tempEP = new IPEndPoint(IPAddress.IPv6Any, 0);
            try
            {
                SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer.Memory, tempEP, cts.Token).ConfigureAwait(false);

                if (buffer.Memory.Span.Slice(0, result.ReceivedBytes).SequenceEqual(authBytes))
                {
                    socket.SendTo(endBytes, result.RemoteEndPoint);
                }
                cts.Cancel();

                return (socket, result.RemoteEndPoint as IPEndPoint);
            }
            catch (Exception)
            {

            }
            socket.SafeClose();
            return (null, null);
        }

    }
}