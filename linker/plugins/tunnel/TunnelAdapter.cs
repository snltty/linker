using linker.client.config;
using linker.config;
using linker.plugins.tunnel.messenger;
using linker.tunnel.adapter;
using linker.tunnel.transport;
using linker.libs;
using MemoryPack;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using linker.plugins.client;
using linker.plugins.messenger;
using linker.plugins.tunnel.excludeip;

namespace linker.plugins.tunnel
{
    public sealed class TunnelAdapter : ITunnelAdapter
    {
        public IPAddress LocalIP => clientSignInState.Connection?.LocalAddress.Address ?? IPAddress.Any;
        public IPEndPoint ServerHost => clientSignInState.Connection?.Address ?? null;
        public X509Certificate2 Certificate => tunnelConfigTransfer.Certificate;

        private readonly ClientSignInState clientSignInState;
        private readonly IMessengerSender messengerSender;
        private readonly TunnelExcludeIPTransfer excludeIPTransfer;
        private readonly ClientConfigTransfer clientConfigTransfer;
        private readonly TunnelConfigTransfer tunnelConfigTransfer;

        public TunnelAdapter(ClientSignInState clientSignInState, IMessengerSender messengerSender, TunnelExcludeIPTransfer excludeIPTransfer, ClientConfigTransfer clientConfigTransfer, TunnelConfigTransfer tunnelConfigTransfer)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.excludeIPTransfer = excludeIPTransfer;
            this.clientConfigTransfer = clientConfigTransfer;
            this.tunnelConfigTransfer = tunnelConfigTransfer;
        }

        public List<TunnelTransportItemInfo> GetTunnelTransports()
        {
            return tunnelConfigTransfer.Transports;
        }
        public void SetTunnelTransports(List<TunnelTransportItemInfo> transports, bool updateVersion)
        {
            tunnelConfigTransfer.SetTransports(transports);
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

    }
}
