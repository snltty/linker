using linker.tunnel.transport;
using linker.libs;
using System.Net;
using linker.tunnel.wanport;
using linker.tunnel;
using linker.messenger;
using linker.messenger.tunnel;
using System.Security.Cryptography.X509Certificates;

namespace linker.plugins.tunnel
{
    /// <summary>
    /// 打洞信标适配
    /// </summary>
    public class TunnelMessengerAdapter
    {
        /// <summary>
        /// 获取信标连接
        /// </summary>
        public Func<IConnection> GetSignConnection { get; set; } = () => { return null; };
        /// <summary>
        /// 获取本地网信息
        /// </summary>
        public Func<NetworkInfo> GetNetwork { get; set; } = () => { return new NetworkInfo(); };
        /// <summary>
        /// ssl
        /// </summary>
        public Func<X509Certificate2> Certificate { get; set; } = () => null;
        /// <summary>
        /// 打洞协议列表，按照这个列表去打洞
        /// </summary>
        public Func<List<TunnelTransportItemInfo>> GetTunnelTransports { get; set; } = () => new List<TunnelTransportItemInfo>();
        /// <summary>
        /// 保存打洞协议列表，因为可能会有新的打洞协议
        /// </summary>
        public Action<List<TunnelTransportItemInfo>, bool> SetTunnelTransports { get; set; } = (list, update) => { };


        private readonly IMessengerSender messengerSender;

        private readonly TunnelExcludeIPTransfer excludeIPTransfer;

        private readonly TunnelWanPortTransfer tunnelWanPortTransfer;
        private readonly TunnelUpnpTransfer tunnelUpnpTransfer;
        private readonly TunnelTransfer tunnelTransfer;


        private readonly TransportTcpPortMap transportTcpPortMap;
        private readonly TransportUdpPortMap transportUdpPortMap;

        private readonly ISerializer serializer;

        public TunnelMessengerAdapter(IMessengerSender messengerSender, TunnelExcludeIPTransfer excludeIPTransfer, TunnelWanPortTransfer tunnelWanPortTransfer, TunnelUpnpTransfer tunnelUpnpTransfer, TunnelTransfer tunnelTransfer, ISerializer serializer)
        {
            this.messengerSender = messengerSender;
            this.excludeIPTransfer = excludeIPTransfer;

            this.tunnelWanPortTransfer = tunnelWanPortTransfer;
            this.tunnelUpnpTransfer = tunnelUpnpTransfer;
            this.tunnelTransfer = tunnelTransfer;

            this.serializer = serializer;

            //加载外网端口
            tunnelWanPortTransfer.LoadTransports(new List<ITunnelWanPortProtocol>
            {
                new TunnelWanPortProtocolLinkerUdp(),
                new TunnelWanPortProtocolLinkerTcp(),
            });

            tunnelTransfer.LocalIP = () => { IConnection connection = GetSignConnection(); return connection?.LocalAddress.Address ?? IPAddress.Any; };
            tunnelTransfer.ServerHost = () => { IConnection connection = GetSignConnection(); return connection?.Address ?? null; };
            tunnelTransfer.Certificate = () => Certificate();
            tunnelTransfer.GetTunnelTransports = () => GetTunnelTransports();
            tunnelTransfer.SetTunnelTransports = (transports, update) => SetTunnelTransports(transports, update);
            tunnelTransfer.GetLocalConfig = GetLocalConfig;
            tunnelTransfer.GetRemoteWanPort = GetRemoteWanPort;
            tunnelTransfer.SendConnectBegin = SendConnectBegin;
            tunnelTransfer.SendConnectFail = SendConnectFail;
            tunnelTransfer.SendConnectSuccess = SendConnectSuccess;
            //加载打洞协议
            transportTcpPortMap = new TransportTcpPortMap();
            transportUdpPortMap = new TransportUdpPortMap();
            tunnelTransfer.LoadTransports(tunnelWanPortTransfer, tunnelUpnpTransfer, new List<ITunnelTransport> {
                new TunnelTransportTcpNutssb(),
                new TransportMsQuic(),
                new TransportTcpP2PNAT(),
                transportTcpPortMap,
                transportUdpPortMap,
                new TransportUdp(),
            });

        }

        private NetworkInfo GetLocalConfig()
        {
            var excludeips = excludeIPTransfer.Get();

            NetworkInfo networkInfo = GetNetwork();
            networkInfo.LocalIps = networkInfo.LocalIps.Where(c =>
            {
                if (c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    uint ip = NetworkHelper.IP2Value(c);
                    foreach (var item in excludeips)
                    {
                        uint maskValue = NetworkHelper.PrefixLength2Value(item.Mask);
                        uint ip1 = NetworkHelper.IP2Value(item.IPAddress);
                        if ((ip & maskValue) == (ip1 & maskValue))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }).ToArray();

            return networkInfo;
        }

        private async Task<TunnelTransportWanPortInfo> GetRemoteWanPort(TunnelWanPortProtocolInfo info)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = GetSignConnection(),
                MessengerId = (ushort)TunnelMessengerIds.InfoForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<TunnelTransportWanPortInfo>(resp.Data.Span);
            }
            return null;
        }
        private async Task<bool> SendConnectBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = GetSignConnection(),
                MessengerId = (ushort)TunnelMessengerIds.BeginForward,
                Payload = serializer.Serialize(tunnelTransportInfo)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        private async Task<bool> SendConnectFail(TunnelTransportInfo tunnelTransportInfo)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = GetSignConnection(),
                MessengerId = (ushort)TunnelMessengerIds.FailForward,
                Payload = serializer.Serialize(tunnelTransportInfo)
            }).ConfigureAwait(false);
            return true;
        }
        private async Task<bool> SendConnectSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = GetSignConnection(),
                MessengerId = (ushort)TunnelMessengerIds.SuccessForward,
                Payload = serializer.Serialize(tunnelTransportInfo)
            }).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// 刷新端口映射，一般来说，端口发生变化，或者网络发生变化就刷新一下
        /// </summary>
        /// <param name="publicPort"></param>
        /// <param name="privatePort"></param>
        public void RefreshPortMap(int publicPort, int privatePort)
        {
            if (privatePort > 0)
            {
                tunnelUpnpTransfer.SetMap(privatePort, publicPort);
                _ = transportTcpPortMap.Listen(privatePort);
                _ = transportUdpPortMap.Listen(privatePort);
            }
            else
            {
                IConnection connection = GetSignConnection();
                if (connection != null && connection.Connected)
                {
                    int ip = connection.LocalAddress.Address.GetAddressBytes()[3];
                    tunnelUpnpTransfer.SetMap(18180 + ip);

                    _ = transportTcpPortMap.Listen(18180 + ip);
                    _ = transportUdpPortMap.Listen(18180 + ip);
                }
            }

        }
    }
}
