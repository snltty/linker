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
        private readonly RelayTestTransfer relayTestTransfer;
        private readonly RelayTransfer relayTransfer;

        public RelayApiController(FileConfig config, RelayTestTransfer relayTestTransfer, RelayTransfer relayTransfer)
        {
            this.config = config;
            this.relayTestTransfer = relayTestTransfer;
            this.relayTransfer = relayTransfer;
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
            config.Data.Client.Relay.Servers = [info];
            config.Data.Update();
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
