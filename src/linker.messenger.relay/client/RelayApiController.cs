using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.api;
using linker.messenger.relay.client.transport;
using linker.messenger.relay.messenger;
using linker.messenger.relay.server;
using linker.messenger.signin;
using linker.messenger.sync;
using linker.tunnel;
using linker.tunnel.connection;
using System.Collections.Concurrent;

namespace linker.messenger.relay.client
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
        public List<RelayServerNodeReportInfo> Subscribe(ApiControllerParamsInfo param)
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
        public RelayOperatingInfo Operating(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (relayTransfer.OperatingVersion.Eq(hashCode, out ulong version) == false)
            {
                return new RelayOperatingInfo
                {
                    List = relayTransfer.Operating,
                    HashCode = version
                };
            }
            return new RelayOperatingInfo { HashCode = version };
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
            //relayClientStore.SetDefaultNodeId(relayConnectInfo.NodeId);
            //relayClientStore.SetDefaultProtocol(relayConnectInfo.Protocol);
            _ = relayTransfer.ConnectAsync(relayConnectInfo.FromMachineId, relayConnectInfo.ToMachineId, relayConnectInfo.TransactionId, relayConnectInfo.NodeId, relayConnectInfo.Protocol);
            return true;
        }

        /// <summary>
        /// 更新节点
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Edit(ApiControllerParamsInfo param)
        {
            RelayServerNodeUpdateInfo info = param.Content.DeJson<RelayServerNodeUpdateInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.EditForward188,
                Payload = serializer.Serialize(new RelayServerNodeUpdateWrapInfo
                {
                    Info = info,
                })
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        /// <summary>
        /// 重启节点
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Exit(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.ExitForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        /// <summary>
        /// 更新节点
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Update(ApiControllerParamsInfo param)
        {
            UpdateInfo info = param.Content.DeJson<UpdateInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.UpdateForward,
                Payload = serializer.Serialize(new KeyValuePair<string, string>(info.Key, info.Value))
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
    }


    public sealed class RelayOperatingInfo
    {
        public ConcurrentDictionary<string, bool> List { get; set; }
        public ulong HashCode { get; set; }
    }

    public sealed class UpdateInfo
    {
        public string Key { get; set; }
        public string Value { get; set; }
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
