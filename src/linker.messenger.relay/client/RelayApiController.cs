using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.api;
using linker.messenger.relay.client;
using linker.messenger.relay.client.transport;
using linker.messenger.relay.messenger;
using linker.messenger.relay.server;
using linker.messenger.signin;
using linker.messenger.sync;
using linker.tunnel.connection;
using System.Collections.Concurrent;

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
        private readonly SyncTreansfer syncTreansfer;

        public RelayApiController(RelayClientTestTransfer relayTestTransfer, RelayClientTransfer relayTransfer, IRelayClientStore relayClientStore,
            SignInClientState signInClientState, IMessengerSender messengerSender, ISerializer serializer, ISignInClientStore signInClientStore, SyncTreansfer syncTreansfer)
        {
            this.relayTestTransfer = relayTestTransfer;
            this.relayTransfer = relayTransfer;
            this.relayClientStore = relayClientStore;
            this.signInClientState = signInClientState;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.signInClientStore = signInClientStore;
            this.syncTreansfer = syncTreansfer;
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

        public KeyValuePairInfo GetDefault(ApiControllerParamsInfo param)
        {
            return new KeyValuePairInfo { Key = relayClientStore.DefaultNodeId, Value = relayClientStore.DefaultProtocol };
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

        /// <summary>
        /// 正在操作列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public ConcurrentDictionary<string, bool> Operating(ApiControllerParamsInfo param)
        {
            return relayTransfer.Operating;
        }
        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool Connect(ApiControllerParamsInfo param)
        {
            RelayConnectInfo relayConnectInfo = param.Content.DeJson<RelayConnectInfo>();
            if (relayConnectInfo.Protocol == TunnelProtocolType.None)
            {
                relayConnectInfo.Protocol = TunnelProtocolType.Tcp;
            }
            _ = relayTransfer.ConnectAsync(relayConnectInfo.FromMachineId, relayConnectInfo.ToMachineId, relayConnectInfo.TransactionId, relayConnectInfo.NodeId, relayConnectInfo.Protocol);
            relayClientStore.SetDefaultNodeId(relayConnectInfo.NodeId);
            relayClientStore.SetDefaultProtocol(relayConnectInfo.Protocol);
            return true;
        }

        /// <summary>
        /// 更新节点
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
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
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        /// <summary>
        /// 检查密钥
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> CheckKey(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.CheckKey,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        /// <summary>
        /// 添加用户到节点
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> AddUser2Node(ApiControllerParamsInfo param)
        {
            RelayServerUser2NodeInfo info = param.Content.DeJson<RelayServerUser2NodeInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.AddUser2Node,
                Payload = serializer.Serialize(new RelayServerUser2NodeAddInfo
                {
                    Data = info,
                    SecretKey = relayClientStore.Server.SecretKey
                })
            }).ConfigureAwait(false);

            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        /// <summary>
        /// 删除用户到节点
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> DelUser2Node(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.DelUser2Node,
                Payload = serializer.Serialize(new RelayServerUser2NodeDelInfo
                {
                    Id = int.Parse(param.Content),
                    SecretKey = relayClientStore.Server.SecretKey
                })
            }).ConfigureAwait(false);

            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        /// <summary>
        /// 用户到节点分页查询
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<RelayServerUser2NodePageResultInfo> PageUser2Node(ApiControllerParamsInfo param)
        {
            RelayServerUser2NodePageRequestInfo info = param.Content.DeJson<RelayServerUser2NodePageRequestInfo>();
            info.SecretKey = relayClientStore.Server.SecretKey;
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.PageUser2Node,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<RelayServerUser2NodePageResultInfo>(resp.Data.Span);
            }

            return new RelayServerUser2NodePageResultInfo();
        }
    }

    public sealed class SyncInfo
    {
        public string[] Ids { get; set; } = [];
        public KeyValuePairInfo Data { get; set; } = new KeyValuePairInfo();
    }

    public sealed class KeyValuePairInfo
    {
        public string Key { get; set; } = string.Empty;
        public TunnelProtocolType Value { get; set; } = TunnelProtocolType.Tcp;
    }

    public sealed class RelayConnectInfo
    {
        public string FromMachineId { get; set; }
        public string ToMachineId { get; set; }
        public string TransactionId { get; set; }
        public string NodeId { get; set; }
        public TunnelProtocolType Protocol { get; set; }
    }

}
