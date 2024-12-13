using linker.plugins.tunnel.messenger;
using linker.tunnel.transport;
using linker.libs;
using MemoryPack;
using System.Net;
using linker.plugins.client;
using linker.plugins.messenger;
using linker.plugins.tunnel.excludeip;
using linker.tunnel.wanport;
using linker.tunnel;

namespace linker.plugins.tunnel
{
    public sealed class TunnelAdapter
    {
        private readonly ClientSignInState clientSignInState;
        private readonly IMessengerSender messengerSender;

        private readonly TunnelExcludeIPTransfer excludeIPTransfer;
        private readonly ClientConfigTransfer clientConfigTransfer;
        private readonly TunnelConfigTransfer tunnelConfigTransfer;

        private readonly TunnelWanPortTransfer tunnelWanPortTransfer;
        private readonly TunnelUpnpTransfer tunnelUpnpTransfer;
        private readonly TunnelTransfer tunnelTransfer;


        private readonly TransportTcpPortMap transportTcpPortMap;
        private readonly TransportUdpPortMap transportUdpPortMap;

        public TunnelAdapter(ClientSignInState clientSignInState, IMessengerSender messengerSender,
            TunnelExcludeIPTransfer excludeIPTransfer, ClientConfigTransfer clientConfigTransfer, TunnelConfigTransfer tunnelConfigTransfer,
            TunnelWanPortTransfer tunnelWanPortTransfer, TunnelUpnpTransfer tunnelUpnpTransfer, TunnelTransfer tunnelTransfer)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.excludeIPTransfer = excludeIPTransfer;
            this.clientConfigTransfer = clientConfigTransfer;
            this.tunnelConfigTransfer = tunnelConfigTransfer;

            this.tunnelWanPortTransfer = tunnelWanPortTransfer;
            this.tunnelUpnpTransfer = tunnelUpnpTransfer;
            this.tunnelTransfer = tunnelTransfer;

            tunnelWanPortTransfer.LoadTransports(new List<ITunnelWanPortProtocol>
            {
                new TunnelWanPortProtocolLinkerUdp(),
                new TunnelWanPortProtocolLinkerTcp(),
            });

            tunnelTransfer.LocalIP = () => clientSignInState.Connection?.LocalAddress.Address ?? IPAddress.Any;
            tunnelTransfer.ServerHost = () => clientSignInState.Connection?.Address ?? null;
            tunnelTransfer.Certificate = () => tunnelConfigTransfer.Certificate;
            tunnelTransfer.GetTunnelTransports = () => tunnelConfigTransfer.Transports;
            tunnelTransfer.SetTunnelTransports = (transports, update) => tunnelConfigTransfer.SetTransports(transports);
            tunnelTransfer.GetLocalConfig = GetLocalConfig;
            tunnelTransfer.GetRemoteWanPort = GetRemoteWanPort;
            tunnelTransfer.SendConnectBegin = SendConnectBegin;
            tunnelTransfer.SendConnectFail = SendConnectFail;
            tunnelTransfer.SendConnectSuccess = SendConnectSuccess;
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

            clientSignInState.NetworkEnabledHandle += (times) => RefreshPortMap();
            tunnelConfigTransfer.OnChanged += RefreshPortMap;
        }

        public NetworkInfo GetLocalConfig()
        {
            var excludeips = excludeIPTransfer.Get();
            return new NetworkInfo
            {
                LocalIps = tunnelConfigTransfer.LocalIPs.Where(c =>
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
                })
                .ToArray(),
                RouteLevel = tunnelConfigTransfer.RouteLevel,
                MachineId = clientConfigTransfer.Id
            };
        }

        public async Task<TunnelTransportWanPortInfo> GetRemoteWanPort(TunnelWanPortProtocolInfo info)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.InfoForward,
                Payload = MemoryPackSerializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return MemoryPackSerializer.Deserialize<TunnelTransportWanPortInfo>(resp.Data.Span);
            }
            return null;
        }

        public async Task<bool> SendConnectBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.BeginForward,
                Payload = MemoryPackSerializer.Serialize(tunnelTransportInfo)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        public async Task<bool> SendConnectFail(TunnelTransportInfo tunnelTransportInfo)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.FailForward,
                Payload = MemoryPackSerializer.Serialize(tunnelTransportInfo)
            }).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> SendConnectSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.SuccessForward,
                Payload = MemoryPackSerializer.Serialize(tunnelTransportInfo)
            }).ConfigureAwait(false);
            return true;
        }


        private void RefreshPortMap()
        {
            if (tunnelConfigTransfer.PortMapLan > 0)
            {
                tunnelUpnpTransfer.SetMap(tunnelConfigTransfer.PortMapLan, tunnelConfigTransfer.PortMapWan);
                _ = transportTcpPortMap.Listen(tunnelConfigTransfer.PortMapLan);
                _ = transportUdpPortMap.Listen(tunnelConfigTransfer.PortMapLan);
            }
            else
            {
                if (clientSignInState.Connected)
                {
                    int ip = clientSignInState.Connection.LocalAddress.Address.GetAddressBytes()[3];
                    tunnelUpnpTransfer.SetMap(18180 + ip);

                    _ = transportTcpPortMap.Listen(18180 + ip);
                    _ = transportUdpPortMap.Listen(18180 + ip);
                }
            }

        }
    }
}
