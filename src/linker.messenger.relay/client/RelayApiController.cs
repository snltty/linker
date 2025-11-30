using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.relay.server;
using linker.messenger.signin;
using linker.messenger.sync;
using linker.tunnel;
using linker.tunnel.connection;
using linker.tunnel.transport;

namespace linker.messenger.relay.client
{
    public sealed class RelayApiController : IApiController
    {
        private readonly RelayClientTestTransfer relayTestTransfer;
        private readonly IRelayClientStore relayClientStore;
        private readonly ISerializer serializer;
        private readonly ISignInClientStore signInClientStore;
        private readonly SyncTreansfer syncTreansfer;
        private readonly TunnelTransfer tunnelTransfer;

        public RelayApiController(RelayClientTestTransfer relayTestTransfer, IRelayClientStore relayClientStore, IMessengerSender messengerSender, ISerializer serializer,
            ISignInClientStore signInClientStore, SyncTreansfer syncTreansfer, TunnelTransfer tunnelTransfer)
        {
            this.relayTestTransfer = relayTestTransfer;
            this.relayClientStore = relayClientStore;
            this.serializer = serializer;
            this.signInClientStore = signInClientStore;
            this.syncTreansfer = syncTreansfer;
            this.tunnelTransfer = tunnelTransfer;
        }

        public List<RelayServerNodeStoreInfo> Subscribe(ApiControllerParamsInfo param)
        {
            relayTestTransfer.Subscribe();
            return relayTestTransfer.Nodes;
        }

        public KeyValueInfo<string, TunnelProtocolType> GetDefault(ApiControllerParamsInfo param)
        {
            return new KeyValueInfo<string, TunnelProtocolType> { Key = relayClientStore.DefaultNodeId, Value = relayClientStore.DefaultProtocol };
        }
        public async Task SyncDefault(ApiControllerParamsInfo param)
        {
            SyncInfo info = param.Content.DeJson<SyncInfo>();
            await syncTreansfer.Sync("RelayDefault", info.Ids, serializer.Serialize(new KeyValuePair<string, TunnelProtocolType>(info.Data.Key, info.Data.Value))).ConfigureAwait(false);
            if (info.Ids.Length == 0 || info.Ids.Contains(signInClientStore.Id))
            {
                relayClientStore.SetDefaultNodeId(info.Data.Key);
                relayClientStore.SetDefaultProtocol(info.Data.Value);
            }
        }

        public bool Connect(ApiControllerParamsInfo param)
        {
            RelayConnectInfo relayConnectInfo = param.Content.DeJson<RelayConnectInfo>();
            _ = tunnelTransfer.ConnectAsync(relayConnectInfo.ToMachineId, relayConnectInfo.TransactionId, TunnelProtocolType.Udp,
                new RelayInfo { NodeId = relayConnectInfo.NodeId }.ToJson(), transportNames: ["TcpRelay"]);
            return true;
        }

    }


    public sealed class SyncInfo
    {
        public string[] Ids { get; set; } = [];
        public KeyValueInfo<string, TunnelProtocolType> Data { get; set; } = new KeyValueInfo<string, TunnelProtocolType> { Key = string.Empty, Value = TunnelProtocolType.Tcp };
    }

    public sealed class RelayConnectInfo
    {
        public string ToMachineId { get; set; }
        public string TransactionId { get; set; }
        public string NodeId { get; set; }
    }
}
