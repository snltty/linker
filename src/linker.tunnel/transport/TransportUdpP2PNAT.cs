
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using linker.tunnel.wanport;

namespace linker.tunnel.transport
{
    /// <summary>
    /// UDP同时连接打洞
    /// 
    /// 大致原理
    /// A 通知 B ，B立马连接A，A也同时去连接B
    /// </summary>
    public sealed class TransportUdpP2PNAT : ITunnelTransport
    {
        public string Name => "UdpP2PNAT";
        public string Label => "UDP、同时打开";
        public TunnelProtocolType ProtocolType => TunnelProtocolType.Udp;
        public TunnelWanPortProtocolType AllowWanPortProtocolType => TunnelWanPortProtocolType.Udp;
        public bool Reverse => true;

        public bool DisableReverse => false;

        public bool SSL => true;

        public bool DisableSSL => false;

        public byte Order => 3;

        /// <summary>
        /// 连接成功
        /// </summary>
        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };


        private readonly byte[] authBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.ttl1");
        private readonly byte[] endBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.end1");

        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;
        public TransportUdpP2PNAT(ITunnelMessengerAdapter tunnelMessengerAdapter)
        {
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }
        public void SetSSL(X509Certificate certificate)
        {
        }

        /// <summary>
        /// 连接对方
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo).ConfigureAwait(false) == false)
            {
                return null;
            }
            if (tunnelTransportInfo.Direction == TunnelDirection.Reverse)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }
            ITunnelConnection connection = await ConnectForward(tunnelTransportInfo, TunnelMode.Client).ConfigureAwait(false);
            if (connection != null)
            {
                await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                return connection;
            }

            await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
            return null;
        }
        /// <summary>
        /// 收到对方开始连接的消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            ITunnelConnection connection = await ConnectForward(tunnelTransportInfo, TunnelMode.Server).ConfigureAwait(false);
            if (connection != null)
            {
                OnConnected(connection);
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

        private async Task<ITunnelConnection> ConnectForward(TunnelTransportInfo tunnelTransportInfo, TunnelMode mode)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {string.Join("\r\n", tunnelTransportInfo.RemoteEndPoints.Select(c => c.ToString()))}");
            }

            IPEndPoint ep = tunnelTransportInfo.Remote.LocalIps.Any(c => c.AddressFamily == AddressFamily.InterNetworkV6)
                && tunnelTransportInfo.Local.LocalIps.Any(c => c.AddressFamily == AddressFamily.InterNetworkV6)
                ? new IPEndPoint(tunnelTransportInfo.Remote.LocalIps.FirstOrDefault(c => c.AddressFamily == AddressFamily.InterNetworkV6), tunnelTransportInfo.Remote.Remote.Port)
                : tunnelTransportInfo.Remote.Remote;

            byte[] buffer = new byte[1024];
            IPEndPoint tempEP = new IPEndPoint(ep.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);
            Socket targetSocket = new(ep.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            targetSocket.IPv6Only(ep.AddressFamily, false);
            targetSocket.WindowsUdpBug();
            targetSocket.ReuseBind(new IPEndPoint(ep.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port));

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
                    }

                    targetSocket.SendTo(authBytes, ep);
                recv:;
                    var result = await targetSocket.ReceiveFromAsync(buffer, tempEP).WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
                    if ((result.RemoteEndPoint as IPEndPoint).Equals(ep) == false)
                    {
                        goto recv;
                    }

                    ISymmetricCrypto crypto = mode == TunnelMode.Client ? CryptoFactory.CreateSymmetric(tunnelTransportInfo.Remote.MachineId) : CryptoFactory.CreateSymmetric(tunnelTransportInfo.Local.MachineId);
                    return new TunnelConnectionUdp
                    {
                        UdpClient = targetSocket,
                        RemoteMachineId = tunnelTransportInfo.Remote.MachineId,
                        RemoteMachineName = tunnelTransportInfo.Remote.MachineName,
                        Direction = tunnelTransportInfo.Direction,
                        ProtocolType = TunnelProtocolType.Udp,
                        Type = TunnelType.P2P,
                        Mode = mode,
                        TransactionId = tunnelTransportInfo.TransactionId,
                        TransactionTag = tunnelTransportInfo.TransactionTag,
                        TransportName = tunnelTransportInfo.TransportName,
                        IPEndPoint = NetworkHelper.TransEndpointFamily(ep),
                        Label = string.Empty,
                        Receive = true,
                        SSL = tunnelTransportInfo.SSL,
                        Crypto = crypto
                    };
                }
                catch (Exception)
                {
                }
            }
            targetSocket.SafeClose();
            return null;
        }
    }
}
