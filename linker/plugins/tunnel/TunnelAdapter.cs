using linker.client;
using linker.client.config;
using linker.config;
using linker.plugins.tunnel.messenger;
using linker.server;
using linker.tunnel.adapter;
using linker.tunnel.transport;
using linker.libs;
using MemoryPack;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using linker.tunnel.wanport;
using System.Linq;
using linker.libs.extends;
using System.Buffers.Binary;

namespace linker.plugins.tunnel
{
    public sealed class TunnelAdapter : ITunnelAdapter
    {
        public IPAddress LocalIP => clientSignInState.Connection?.LocalAddress.Address ?? IPAddress.Any;
        public X509Certificate2 Certificate { get; private set; }


        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;
        private readonly ConfigWrap config;
        private readonly RunningConfig running;

        public TunnelAdapter(ClientSignInState clientSignInState, MessengerSender messengerSender, ConfigWrap config, RunningConfig running)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.config = config;
            this.running = running;


            string path = Path.GetFullPath(config.Data.Client.Certificate);
            if (File.Exists(path))
            {
                Certificate = new X509Certificate2(path, config.Data.Client.Password, X509KeyStorageFlags.Exportable);
            }
        }

        public List<TunnelWanPortInfo> GetTunnelWanPortCompacts()
        {
            return running.Data.Tunnel.Servers.ToList();
        }
        public void SetTunnelWanPortCompacts(List<TunnelWanPortInfo> compacts)
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
                LocalIps = config.Data.Client.Tunnel.LocalIPs
                .Where(c => c.Equals(running.Data.Tuntap.IP) == false)
                .Where(c=>
                {
                    if(c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        uint ip = BinaryPrimitives.ReadUInt32BigEndian(c.GetAddressBytes());
                        foreach (var item in running.Data.Tunnel.ExcludeIPs)
                        {
                            uint maskValue = NetworkHelper.MaskValue(item.Mask);
                            uint ip1 = BinaryPrimitives.ReadUInt32BigEndian(item.IPAddress.GetAddressBytes());
                            if((ip & maskValue) == (ip1 & maskValue))
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
        public async Task<TunnelTransportWanPortInfo> GetRemoteWanPort(string remoteMachineId)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.InfoForward,
                Payload = MemoryPackSerializer.Serialize(remoteMachineId)
            });
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
