using linker.config;
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
        private readonly ClientSignInState clientSignInState;
        private readonly IMessengerSender messengerSender;
        private readonly RelayTestTransfer relayTestTransfer;

        public RelayApiController(FileConfig config,RelayTestTransfer relayTestTransfer)
        {
            this.config = config;
            this.relayTestTransfer = relayTestTransfer;
        }
       
        /// <summary>
        /// 设置中继协议
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [ClientApiAccessAttribute(ClientApiAccess.Config)]
        public bool SetServers(ApiControllerParamsInfo param)
        {
            RelayServerInfo info = param.Content.DeJson<RelayServerInfo>();
            config.Data.Client.Relay.Servers = [info];
            config.Data.Update();
            return true;
        }

        public void Subscribe(ApiControllerParamsInfo param)
        {
            relayTestTransfer.Subscribe();
        }
    }

}
