using linker.config;
using linker.plugins.relay.messenger;
using linker.libs.api;
using linker.libs.extends;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;

namespace linker.plugins.relay
{
    /// <summary>
    /// 中继管理接口
    /// </summary>
    public sealed class RelayApiController : IApiClientController
    {
        private readonly FileConfig config;
        private readonly RelayTransfer relayTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;

        public RelayApiController(FileConfig config, RelayTransfer relayTransfer, ClientSignInState clientSignInState, MessengerSender messengerSender)
        {
            this.config = config;
            this.relayTransfer = relayTransfer;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
        }
        /// <summary>
        /// 获取所有中继协议
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public List<RelayTypeInfo> GetTypes(ApiControllerParamsInfo param)
        {
            return relayTransfer.GetTypes();
        }
        /// <summary>
        /// 设置中继协议
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [ClientApiAccessAttribute(ClientApiAccess.Config)]
        public bool SetServers(ApiControllerParamsInfo param)
        {
            RelayServerInfo[] info = param.Content.DeJson<RelayServerInfo[]>();
            relayTransfer.SetServers(info);
            return true;
        }

        public void Subscribe(ApiControllerParamsInfo param)
        {
            relayTransfer.SubscribeDelayTest();
        }
    }

}
