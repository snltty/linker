using cmonitor.client;
using cmonitor.client.config;
using cmonitor.config;
using cmonitor.plugins.tunnel.messenger;
using cmonitor.server;
using cmonitor.tunnel.adapter;
using cmonitor.tunnel.compact;
using cmonitor.tunnel.transport;
using common.libs;
using MemoryPack;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace cmonitor.plugins.tunnel
{
    public sealed class TunnelAdapter : ITunnelAdapter
    {
        public IPAddress LocalIP => clientSignInState.Connection?.LocalAddress.Address ?? IPAddress.Any;
        public X509Certificate Certificate { get; private set; }

        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;
        private readonly Config config;
        private readonly RunningConfig running;

        public TunnelAdapter(ClientSignInState clientSignInState, MessengerSender messengerSender, Config config, RunningConfig running)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.config = config;
            this.running = running;


            string path = Path.GetFullPath(config.Data.Client.Certificate);
            if (File.Exists(path))
            {
                Certificate = new X509Certificate(path, config.Data.Client.Password);
            }
        }

        public List<TunnelCompactInfo> GetTunnelCompacts()
        {
            return running.Data.Tunnel.Servers.ToList();
        }
        public void SetTunnelCompacts(List<TunnelCompactInfo> compacts)
        {
            running.Data.Tunnel.Servers = compacts.ToArray();
            running.Data.Update();
        }

        public List<TunnelTransportItemInfo> GetTunnelTransports()
        {
            return running.Data.Tunnel.Transports;
        }
        public void SetTunnelTransports(List<TunnelTransportItemInfo> transports)
        {
            running.Data.Tunnel.Transports = transports;
            running.Data.Update();
        }

        public NetworkInfo GetLocalConfig()
        {
            return new NetworkInfo
            {
                LocalIps = config.Data.Client.Tunnel.LocalIPs,
                RouteLevel = config.Data.Client.Tunnel.RouteLevel + running.Data.Tunnel.RouteLevelPlus,
                MachineName = config.Data.Client.Name
            };
        }
        public async Task<TunnelTransportExternalIPInfo> GetRemoteExternalIP(string remoteMachineName)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.InfoForward,
                Payload = MemoryPackSerializer.Serialize(remoteMachineName)
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return MemoryPackSerializer.Deserialize<TunnelTransportExternalIPInfo>(resp.Data.Span);
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
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        public async Task<bool> SendConnectFail(TunnelTransportInfo tunnelTransportInfo)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.FailForward,
                Payload = MemoryPackSerializer.Serialize(tunnelTransportInfo)
            });
            return true;
        }

        public async Task<bool> SendConnectSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.SuccessForward,
                Payload = MemoryPackSerializer.Serialize(tunnelTransportInfo)
            });
            return true;
        }


    }
}
