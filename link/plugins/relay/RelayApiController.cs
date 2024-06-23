using link.client;
using link.client.capi;
using link.config;
using link.plugins.relay.messenger;
using link.server;
using link.libs.api;
using link.libs.extends;
using MemoryPack;

namespace link.plugins.relay
{
    /// <summary>
    /// 中继管理接口
    /// </summary>
    public sealed class RelayApiController : IApiClientController
    {
        private readonly Config config;
        private readonly RelayTransfer relayTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;

        public RelayApiController(Config config, RelayTransfer relayTransfer, ClientSignInState clientSignInState, MessengerSender messengerSender)
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
        public List<RelayCompactTypeInfo> GetTypes(ApiControllerParamsInfo param)
        {
            return relayTransfer.GetTypes();
        }
        /// <summary>
        /// 设置中继协议
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> SetServers(ApiControllerParamsInfo param)
        {
            RelayCompactParamInfo info = param.Content.DeJson<RelayCompactParamInfo>();

            relayTransfer.OnServers(info.List);

            if (info.Sync)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)RelayMessengerIds.ServersForward,
                    Payload = MemoryPackSerializer.Serialize(info.List)
                });
            }


            return true;
        }
    }

    public sealed class RelayCompactParamInfo
    {
        public bool Sync { get; set; }
        public RelayCompactInfo[] List { get; set; } = Array.Empty<RelayCompactInfo>();
    }

}
