using linker.client.config;
using linker.config;
using linker.libs.api;
using linker.libs.extends;
using linker.plugins.capi;

namespace linker.plugins.relay.client
{
    /// <summary>
    /// 中继管理接口
    /// </summary>
    public sealed class RelayApiController : IApiClientController
    {
        private readonly FileConfig config;
        private readonly RunningConfig runningConfig;
        private readonly RelayTestTransfer relayTestTransfer;
        private readonly RelayTransfer relayTransfer;
        private readonly RelayClientConfigTransfer relayClientConfigTransfer;

        public RelayApiController(FileConfig config, RunningConfig runningConfig, RelayTestTransfer relayTestTransfer, RelayTransfer relayTransfer, RelayClientConfigTransfer relayClientConfigTransfer)
        {
            this.config = config;
            this.runningConfig = runningConfig;
            this.relayTestTransfer = relayTestTransfer;
            this.relayTransfer = relayTransfer;
            this.relayClientConfigTransfer = relayClientConfigTransfer;
        }

        /// <summary>
        /// 设置中继协议
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [ClientApiAccess(ClientApiAccess.Config)]
        public bool SetServers(ApiControllerParamsInfo param)
        {
            RelayServerInfo info = param.Content.DeJson<RelayServerInfo>();
            relayClientConfigTransfer.SetServer(info);
            return true;
        }

        public List<RelayNodeReportInfo> Subscribe(ApiControllerParamsInfo param)
        {
            relayTestTransfer.Subscribe();
            return relayTestTransfer.Nodes;
        }

        public bool Connect(ApiControllerParamsInfo param)
        {
            RelayConnectInfo relayConnectInfo = param.Content.DeJson<RelayConnectInfo>();
            _ = relayTransfer.ConnectAsync(relayConnectInfo.FromMachineId, relayConnectInfo.ToMachineId, relayConnectInfo.TransactionId, relayConnectInfo.NodeId);
            relayClientConfigTransfer.SetDefaultNodeId(relayConnectInfo.NodeId);
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
