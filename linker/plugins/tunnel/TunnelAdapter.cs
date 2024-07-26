using linker.client.config;
using linker.config;
using linker.plugins.tunnel.messenger;
using linker.tunnel.adapter;
using linker.tunnel.transport;
using linker.libs;
using MemoryPack;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using linker.tunnel.wanport;
using System.Buffers.Binary;
using linker.plugins.client;
using linker.plugins.messenger;
using linker.plugins.tunnel.excludeip;

namespace linker.plugins.tunnel
{
    public sealed class TunnelAdapter : ITunnelAdapter
    {
        public IPAddress LocalIP => clientSignInState.Connection?.LocalAddress.Address ?? IPAddress.Any;
        public X509Certificate2 Certificate { get; private set; }
        public PortMapInfo PortMap => new PortMapInfo { WanPort = running.Data.Tunnel.PortMapWan, LanPort = running.Data.Tunnel.PortMapLan };

        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;
        private readonly FileConfig config;
        private readonly RunningConfig running;
        private readonly RunningConfigTransfer runningConfigTransfer;
        private readonly TunnelExcludeIPTransfer excludeIPTransfer;

        private string wanPortConfigKey = "tunnelWanPortProtocols";
        private string transportConfigKey = "tunnelTransports";

        public TunnelAdapter(ClientSignInState clientSignInState, MessengerSender messengerSender, FileConfig config, RunningConfig running, RunningConfigTransfer runningConfigTransfer, TunnelExcludeIPTransfer excludeIPTransfer)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.config = config;
            this.running = running;
            this.runningConfigTransfer = runningConfigTransfer;
            this.excludeIPTransfer = excludeIPTransfer;

            string path = Path.GetFullPath(config.Data.Client.Certificate);
            if (File.Exists(path))
            {
                Certificate = new X509Certificate2(path, config.Data.Client.Password, X509KeyStorageFlags.Exportable);
            }


            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                SyncWanPort();
                SyncTransport();
            };
            runningConfigTransfer.Setter(wanPortConfigKey, SetTunnelWanPortProtocols);
            runningConfigTransfer.Getter(wanPortConfigKey, () => MemoryPackSerializer.Serialize(GetTunnelWanPortProtocols()));

            runningConfigTransfer.Setter(transportConfigKey, SetTunnelTransports);
            runningConfigTransfer.Getter(transportConfigKey, () => MemoryPackSerializer.Serialize(GetTunnelTransports()));
        }

        private void SyncWanPort()
        {
            runningConfigTransfer.Sync(wanPortConfigKey, MemoryPackSerializer.Serialize(GetTunnelWanPortProtocols()));
        }
        private void SyncTransport()
        {
            runningConfigTransfer.Sync(transportConfigKey, MemoryPackSerializer.Serialize(GetTunnelTransports()));
        }
        public List<TunnelWanPortInfo> GetTunnelWanPortProtocols()
        {
            return running.Data.Tunnel.Servers;
        }
        public void SetTunnelWanPortProtocols(List<TunnelWanPortInfo> compacts, bool updateVersion)
        {
            running.Data.Tunnel.Servers = compacts;
            running.Data.Update();
            if (updateVersion)
                runningConfigTransfer.IncrementVersion(wanPortConfigKey);
            SyncWanPort();
        }
        private void SetTunnelWanPortProtocols(Memory<byte> data)
        {
            running.Data.Tunnel.Servers = MemoryPackSerializer.Deserialize<List<TunnelWanPortInfo>>(data.Span);
            running.Data.Update();
        }

        public List<TunnelTransportItemInfo> GetTunnelTransports()
        {
            return running.Data.Tunnel.Transports;
        }
        public void SetTunnelTransports(List<TunnelTransportItemInfo> transports, bool updateVersion)
        {
            running.Data.Tunnel.Transports = transports;
            running.Data.Update();
            if (updateVersion)
                runningConfigTransfer.IncrementVersion(transportConfigKey);
            SyncTransport();
        }
        private void SetTunnelTransports(Memory<byte> data)
        {
            running.Data.Tunnel.Transports = MemoryPackSerializer.Deserialize<List<TunnelTransportItemInfo>>(data.Span);
            running.Data.Update();
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
                        uint ip = BinaryPrimitives.ReadUInt32BigEndian(c.GetAddressBytes());
                        foreach (var item in excludeips)
                        {
                            uint maskValue = NetworkHelper.MaskValue(item.Mask);
                            uint ip1 = BinaryPrimitives.ReadUInt32BigEndian(item.IPAddress.GetAddressBytes());
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
