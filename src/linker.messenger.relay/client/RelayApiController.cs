using linker.libs;
using linker.libs.api;
using linker.libs.extends;
using linker.messenger.api;
using linker.messenger.relay.client;
using linker.messenger.relay.client.transport;
using linker.messenger.relay.messenger;
using linker.messenger.relay.server;
using linker.messenger.signin;

namespace linker.messenger.relay
{
    /// <summary>
    /// 中继管理接口
    /// </summary>
    public sealed class RelayApiController : IApiController
    {
        private readonly RelayClientTestTransfer relayTestTransfer;
        private readonly RelayClientTransfer relayTransfer;
        private readonly IRelayClientStore relayClientStore;
        private readonly SignInClientState signInClientState;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly ISignInClientStore signInClientStore;

        public RelayApiController(RelayClientTestTransfer relayTestTransfer, RelayClientTransfer relayTransfer, IRelayClientStore relayClientStore,
            SignInClientState signInClientState, IMessengerSender messengerSender, ISerializer serializer, ISignInClientStore signInClientStore)
        {
            this.relayTestTransfer = relayTestTransfer;
            this.relayTransfer = relayTransfer;
            this.relayClientStore = relayClientStore;
            this.signInClientState = signInClientState;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.signInClientStore = signInClientStore;
        }

        [Access(AccessValue.Config)]
        public bool SetServers(ApiControllerParamsInfo param)
        {
            RelayServerInfo info = param.Content.DeJson<RelayServerInfo>();
            relayClientStore.SetServer(info);
            return true;
        }

        public List<RelayServerNodeReportInfo170> Subscribe(ApiControllerParamsInfo param)
        {
            relayTestTransfer.Subscribe();
            return relayTestTransfer.Nodes;
        }

        public bool Connect(ApiControllerParamsInfo param)
        {
            RelayConnectInfo relayConnectInfo = param.Content.DeJson<RelayConnectInfo>();
            _ = relayTransfer.ConnectAsync(relayConnectInfo.FromMachineId, relayConnectInfo.ToMachineId, relayConnectInfo.TransactionId, relayConnectInfo.NodeId);
            relayClientStore.SetDefaultNodeId(relayConnectInfo.NodeId);
            return true;
        }


        public async Task<bool> AccessCdkey(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.AccessCdkey,
                Payload = serializer.Serialize(relayClientStore.Server.SecretKey)
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        [Access(AccessValue.RelayCdkey)]
        public async Task<bool> AddCdkey(ApiControllerParamsInfo param)
        {
            RelayServerCdkeyStoreInfo info = param.Content.DeJson<RelayServerCdkeyStoreInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.AddCdkey,
                Payload = serializer.Serialize(new RelayServerCdkeyAddInfo
                {
                    Data = info,
                    SecretKey = relayClientStore.Server.SecretKey
                })
            });

            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        [Access(AccessValue.RelayCdkey)]
        public async Task<bool> DelCdkey(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.DelCdkey,
                Payload = serializer.Serialize(new RelayServerCdkeyDelInfo
                {
                    Id = int.Parse(param.Content),
                    UserId = signInClientStore.Server.UserId,
                    SecretKey = relayClientStore.Server.SecretKey
                })
            });

            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        [Access(AccessValue.RelayCdkey)]
        public async Task<RelayServerCdkeyPageResultInfo> PageCdkey(ApiControllerParamsInfo param)
        {
            RelayServerCdkeyPageRequestInfo info = param.Content.DeJson<RelayServerCdkeyPageRequestInfo>();
            info.SecretKey = relayClientStore.Server.SecretKey;
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.PageCdkey,
                Payload = serializer.Serialize(info)
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<RelayServerCdkeyPageResultInfo>(resp.Data.Span);
            }

            return new RelayServerCdkeyPageResultInfo();
        }

        public async Task<RelayServerCdkeyPageResultInfo> MyCdkey(ApiControllerParamsInfo param)
        {
            RelayServerCdkeyPageRequestInfo info = param.Content.DeJson<RelayServerCdkeyPageRequestInfo>();
            info.SecretKey = relayClientStore.Server.SecretKey;
            info.UserId = signInClientStore.Server.UserId;
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.PageCdkey,
                Payload = serializer.Serialize(info)
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<RelayServerCdkeyPageResultInfo>(resp.Data.Span);
            }

            return new RelayServerCdkeyPageResultInfo();
        }

        public async Task<RelayServerCdkeyTestResultInfo> TestCdkey(ApiControllerParamsInfo param)
        {
            RelayServerCdkeyImportInfo info = param.Content.DeJson<RelayServerCdkeyImportInfo>();
            info.SecretKey = relayClientStore.Server.SecretKey;
            info.UserId = signInClientStore.Server.UserId;
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.TestCdkey,
                Payload = serializer.Serialize(info)
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<RelayServerCdkeyTestResultInfo>(resp.Data.Span);
            }

            return new RelayServerCdkeyTestResultInfo();
        }

        public async Task<string> ImportCdkey(ApiControllerParamsInfo param)
        {
            RelayServerCdkeyImportInfo info = param.Content.DeJson<RelayServerCdkeyImportInfo>();
            info.SecretKey = relayClientStore.Server.SecretKey;
            info.UserId = signInClientStore.Server.UserId;
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.ImportCdkey,
                Payload = serializer.Serialize(info)
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<string>(resp.Data.Span);
            }
            return "Network";
        }
        public async Task<bool> UpdateNode(ApiControllerParamsInfo param)
        {
            RelayServerNodeUpdateInfo info = param.Content.DeJson<RelayServerNodeUpdateInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.UpdateNodeForward,
                Payload = serializer.Serialize(new RelayServerNodeUpdateWrapInfo
                {
                    Info = info,
                    SecretKey = relayClientStore.Server.SecretKey
                })
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
    }

    public sealed class RelayConnectInfo
    {
        public string FromMachineId { get; set; }
        public string ToMachineId { get; set; }
        public string TransactionId { get; set; }
        public string NodeId { get; set; }
    }

}
