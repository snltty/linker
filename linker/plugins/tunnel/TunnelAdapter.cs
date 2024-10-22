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
        public string ServerHost => config.Data.Client.ServerInfo.Host;

        public X509Certificate2 Certificate { get; private set; }

       

        private readonly ClientSignInState clientSignInState;
        private readonly IMessengerSender messengerSender;
        private readonly FileConfig config;
        private readonly RunningConfig running;
        private readonly TunnelExcludeIPTransfer excludeIPTransfer;

        public TunnelAdapter(ClientSignInState clientSignInState, IMessengerSender messengerSender, FileConfig config, RunningConfig running, TunnelExcludeIPTransfer excludeIPTransfer)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.config = config;
            this.running = running;
            this.excludeIPTransfer = excludeIPTransfer;

            string path = Path.GetFullPath(config.Data.Client.SSL.File);
            if (File.Exists(path))
            {
                Certificate = new X509Certificate2(path, config.Data.Client.SSL.Password, X509KeyStorageFlags.Exportable);
            }
        }

        public List<TunnelTransportItemInfo> GetTunnelTransports()
        {
            return config.Data.Client.Tunnel.Transports;
        }
        public void SetTunnelTransports(List<TunnelTransportItemInfo> transports, bool updateVersion)
        {
            config.Data.Client.Tunnel.Transports = transports;
            config.Data.Update();
        }

        public NetworkInfo GetLocalConfig()
        {
            var excludeips = excludeIPTransfer.Get();
            return new NetworkInfo
            {
                LocalIps = config.Data.Client.Tunnel.LocalIPs.Where(c =>
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
                RouteLevel = config.Data.Client.Tunnel.RouteLevel + running.Data.Tunnel.RouteLevelPlus,
                MachineId = config.Data.Client.Id
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
