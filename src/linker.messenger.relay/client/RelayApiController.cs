using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.node;
using linker.messenger.relay.messenger;
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
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly ISignInClientStore signInClientStore;
        private readonly SyncTreansfer syncTreansfer;
        private readonly TunnelTransfer tunnelTransfer;
        private readonly SignInClientState signInClientState;


        public RelayApiController(RelayClientTestTransfer relayTestTransfer, IRelayClientStore relayClientStore, IMessengerSender messengerSender, ISerializer serializer,
            ISignInClientStore signInClientStore, SyncTreansfer syncTreansfer, TunnelTransfer tunnelTransfer, SignInClientState signInClientState)
        {
            this.relayTestTransfer = relayTestTransfer;
            this.relayClientStore = relayClientStore;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.signInClientStore = signInClientStore;
            this.syncTreansfer = syncTreansfer;
            this.tunnelTransfer = tunnelTransfer;
            this.signInClientState = signInClientState;
        }

        public List<RelayServerNodeStoreInfo> Subscribe(ApiControllerParamsInfo param)
        {
            relayTestTransfer.Subscribe();
            return relayTestTransfer.Nodes;
        }
        public bool Connect(ApiControllerParamsInfo param)
        {
            RelayConnectInfo relayConnectInfo = param.Content.DeJson<RelayConnectInfo>();
            _ = tunnelTransfer.ConnectAsync(relayConnectInfo.ToMachineId, relayConnectInfo.TransactionId, TunnelProtocolType.Udp,
                new RelayInfo { NodeId = relayConnectInfo.NodeId }.ToJson(), flag: "relay", tunnelTypes: [TunnelType.Relay]);
            return true;
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
                relayClientStore.Confirm();
            }
        }


        public async Task<string> Share(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.ShareForward,
                Payload = serializer.Serialize(param.Content)
            });
            return resp.Code == MessageResponeCodes.OK ? serializer.Deserialize<string>(resp.Data.Span) : $"network error:{resp.Code}";
        }
        public async Task<string> Import(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.Import,
                Payload = serializer.Serialize(param.Content)
            });
            return resp.Code == MessageResponeCodes.OK ? serializer.Deserialize<string>(resp.Data.Span) : $"network error:{resp.Code}";
        }
        public async Task<string> Remove(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.Remove,
                Payload = serializer.Serialize(param.Content)
            });
            return resp.Code == MessageResponeCodes.OK ? serializer.Deserialize<string>(resp.Data.Span) : $"network error:{resp.Code}";
        }

        public async Task<bool> Update(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.UpdateForward,
                Payload = serializer.Serialize(param.Content.DeJson<RelayServerNodeStoreInfo>())
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        public async Task<bool> Upgrade(ApiControllerParamsInfo param)
        {
            KeyValueInfo<string, string> info = param.Content.DeJson<KeyValueInfo<string, string>>();

            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.UpgradeForward,
                Payload = serializer.Serialize(new KeyValuePair<string, string>(info.Key, info.Value))
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> Exit(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.ExitForward,
                Payload = serializer.Serialize(param.Content)
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        public async Task<MastersResponseInfo> Masters(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.MastersForward,
                Payload = serializer.Serialize(param.Content.DeJson<MastersRequestInfo>())
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<MastersResponseInfo>(resp.Data.Span);
            }
            return new MastersResponseInfo();
        }
        public async Task<MasterDenyStoreResponseInfo> Denys(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.DenysForward,
                Payload = serializer.Serialize(param.Content.DeJson<MasterDenyStoreRequestInfo>())
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<MasterDenyStoreResponseInfo>(resp.Data.Span);
            }
            return new MasterDenyStoreResponseInfo();
        }
        public async Task<bool> DenysAdd(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.DenysAddForward,
                Payload = serializer.Serialize(param.Content.DeJson<MasterDenyAddInfo>())
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> DenysDel(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.DenysDelForward,
                Payload = serializer.Serialize(param.Content.DeJson<MasterDenyDelInfo>())
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
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
