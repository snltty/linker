using cmonitor.client;
using cmonitor.client.capi;
using cmonitor.config;
using cmonitor.plugins.tunnel.messenger;
using cmonitor.server;
using cmonitor.tunnel.adapter;
using cmonitor.tunnel.compact;
using cmonitor.tunnel.transport;
using common.libs.api;
using common.libs.extends;
using MemoryPack;
using System.Collections.Concurrent;

namespace cmonitor.plugins.tunnel
{
    public sealed class TunnelApiController : IApiClientController
    {
        private readonly Config config;
        private readonly TunnelCompactTransfer compactTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;
        private readonly TunnelConfigTransfer tunnelConfigTransfer;
        private readonly ITunnelAdapter tunnelMessengerAdapter;

        public TunnelApiController(Config config, TunnelCompactTransfer compactTransfer, ClientSignInState clientSignInState, MessengerSender messengerSender, TunnelConfigTransfer tunnelConfigTransfer, ITunnelAdapter tunnelMessengerAdapter)
        {
            this.config = config;
            this.compactTransfer = compactTransfer;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.tunnelConfigTransfer = tunnelConfigTransfer;
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }

        public TunnelListInfo Get(ApiControllerParamsInfo param)
        {
            uint hashCode = uint.Parse(param.Content);
            uint _hashCode = tunnelConfigTransfer.ConfigVersion;
            if (_hashCode != hashCode)
            {
                return new TunnelListInfo
                {
                    List = tunnelConfigTransfer.Config,
                    HashCode = _hashCode
                };
            }
            return new TunnelListInfo { HashCode = _hashCode };
        }
        public void Refresh(ApiControllerParamsInfo param)
        {
            tunnelConfigTransfer.RefreshConfig();
        }


        public List<TunnelCompactTypeInfo> GetTypes(ApiControllerParamsInfo param)
        {
            return compactTransfer.GetTypes();
        }

        public async Task<bool> SetServers(ApiControllerParamsInfo param)
        {
            SetServersParamInfo info = param.Content.DeJson<SetServersParamInfo>();

            tunnelMessengerAdapter.SetTunnelCompacts(info.List.ToList());
            if (info.Sync)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.ServersForward,
                    Payload = MemoryPackSerializer.Serialize(info.List)
                });
            }

            return true;

        }

        public async Task<bool> SetRouteLevel(ApiControllerParamsInfo param)
        {
            TunnelTransportRouteLevelInfo tunnelTransportConfigWrapInfo = param.Content.DeJson<TunnelTransportRouteLevelInfo>();

            if (tunnelTransportConfigWrapInfo.MachineId == config.Data.Client.Id)
            {
                tunnelConfigTransfer.OnLocalRouteLevel(tunnelTransportConfigWrapInfo);
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.RouteLevelForward,
                    Payload = MemoryPackSerializer.Serialize(tunnelTransportConfigWrapInfo)
                });
            }

            return true;
        }

        public List<TunnelTransportItemInfo> GetTransports(ApiControllerParamsInfo param)
        {
            return tunnelMessengerAdapter.GetTunnelTransports();
        }
        public async Task SetTransports(ApiControllerParamsInfo param)
        {
            SetTransportsParamInfo info = param.Content.DeJson<SetTransportsParamInfo>();
            tunnelMessengerAdapter.SetTunnelTransports(info.List);
            if (info.Sync)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.TransportForward,
                    Payload = MemoryPackSerializer.Serialize(info.List)
                });
            }
        }


        public sealed class TunnelListInfo
        {
            public ConcurrentDictionary<string, TunnelTransportRouteLevelInfo> List { get; set; }
            public uint HashCode { get; set; }
        }

        public sealed class SetServersParamInfo
        {
            public bool Sync { get; set; }
            public TunnelCompactInfo[] List { get; set; } = Array.Empty<TunnelCompactInfo>();
        }

        public sealed class SetTransportsParamInfo
        {
            public bool Sync { get; set; }
            public List<TunnelTransportItemInfo> List { get; set; } = new List<TunnelTransportItemInfo>();
        }
    }

}
