using cmonitor.client;
using cmonitor.client.capi;
using cmonitor.config;
using cmonitor.plugins.tunnel.compact;
using cmonitor.plugins.tunnel.messenger;
using cmonitor.plugins.tunnel.transport;
using cmonitor.server;
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
        private readonly TunnelTransfer tunnelTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;

        public TunnelApiController(Config config, TunnelCompactTransfer compactTransfer, TunnelTransfer tunnelTransfer, ClientSignInState clientSignInState, MessengerSender messengerSender)
        {
            this.config = config;
            this.compactTransfer = compactTransfer;
            this.tunnelTransfer = tunnelTransfer;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
        }

        public TunnelListInfo Get(ApiControllerParamsInfo param)
        {
            uint hashCode = uint.Parse(param.Content);
            uint _hashCode = tunnelTransfer.ConfigVersion;
            if (_hashCode != hashCode)
            {
                return new TunnelListInfo
                {
                    List = tunnelTransfer.Config,
                    HashCode = _hashCode
                };
            }
            return new TunnelListInfo { HashCode = _hashCode };
        }
        public void Refresh(ApiControllerParamsInfo param)
        {
            tunnelTransfer.RefreshConfig();
        }


        public List<TunnelCompactTypeInfo> GetTypes(ApiControllerParamsInfo param)
        {
            return compactTransfer.GetTypes();
        }

        public bool SetServers(ApiControllerParamsInfo param)
        {
            config.Data.Client.Tunnel.Servers = param.Content.DeJson<TunnelCompactInfo[]>();
            config.Save();
            return true;
        }

        public async Task<bool> SetConfig(ApiControllerParamsInfo param)
        {
            TunnelTransportConfigInfo tunnelTransportConfigWrapInfo = param.Content.DeJson<TunnelTransportConfigInfo>();

            if (tunnelTransportConfigWrapInfo.MachineName == config.Data.Client.Name)
            {
                tunnelTransfer.OnUpdate(tunnelTransportConfigWrapInfo);
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.UpdateForward,
                    Payload = MemoryPackSerializer.Serialize(tunnelTransportConfigWrapInfo)
                });
            }

            return true;
        }

        public sealed class TunnelListInfo
        {
            public ConcurrentDictionary<string, TunnelTransportConfigInfo> List { get; set; }
            public uint HashCode { get; set; }
        }
    }

}
