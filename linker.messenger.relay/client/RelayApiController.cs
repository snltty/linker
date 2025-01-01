using linker.libs.api;
using linker.libs.extends;
using linker.messenger.api;
using linker.messenger.relay.client;
using linker.messenger.relay.client.transport;
using linker.messenger.relay.server;

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

        public RelayApiController(RelayClientTestTransfer relayTestTransfer, RelayClientTransfer relayTransfer, IRelayClientStore relayClientStore)
        {
            this.relayTestTransfer = relayTestTransfer;
            this.relayTransfer = relayTransfer;
            this.relayClientStore = relayClientStore;
        }

        /// <summary>
        /// 设置中继协议
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
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

        public bool Connect(ApiControllerParamsInfo param)
        {
            RelayConnectInfo relayConnectInfo = param.Content.DeJson<RelayConnectInfo>();
            _ = relayTransfer.ConnectAsync(relayConnectInfo.FromMachineId, relayConnectInfo.ToMachineId, relayConnectInfo.TransactionId, relayConnectInfo.NodeId);
            relayClientStore.SetDefaultNodeId(relayConnectInfo.NodeId);
            return true;
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
