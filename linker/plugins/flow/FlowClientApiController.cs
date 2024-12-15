using linker.libs.api;
using linker.config;
using MemoryPack;
using linker.client.config;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;
using linker.plugins.flow.messenger;
using linker.libs.extends;
using linker.plugins.relay.client;
using linker.plugins.sforward;
using linker.messenger;

namespace linker.plugins.flow
{
    public sealed class FlowClientApiController : IApiClientController
    {
        private readonly IMessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;
        private readonly RunningConfig runningConfig;
        private readonly RelayClientConfigTransfer relayClientConfigTransfer;
        private readonly SForwardTransfer sForwardTransfer;

        public FlowClientApiController(IMessengerSender messengerSender, ClientSignInState clientSignInState, FileConfig config, RunningConfig runningConfig, RelayClientConfigTransfer relayClientConfigTransfer, SForwardTransfer sForwardTransfer)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.runningConfig = runningConfig;
            this.relayClientConfigTransfer = relayClientConfigTransfer;
            this.sForwardTransfer = sForwardTransfer;
        }

        public async Task<FlowInfo> GetFlows(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)FlowMessengerIds.List,
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return MemoryPackSerializer.Deserialize<FlowInfo>(resp.Data.Span);
            }
            return new FlowInfo();
        }
        public async Task<Dictionary<ushort, FlowItemInfo>> GetMessengerFlows(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)FlowMessengerIds.Messenger,
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return MemoryPackSerializer.Deserialize<Dictionary<ushort, FlowItemInfo>>(resp.Data.Span);
            }
            return new Dictionary<ushort, FlowItemInfo>();
        }

        [ClientApiAccessAttribute(ClientApiAccess.SForwardFlow)]
        public async Task<SForwardFlowResponseInfo> GetSForwardFlows(ApiControllerParamsInfo param)
        {
            SForwardFlowRequestInfo info = param.Content.DeJson<SForwardFlowRequestInfo>();
            info.SecretKey = sForwardTransfer.SecretKey;

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)FlowMessengerIds.SForward,
                Payload = MemoryPackSerializer.Serialize(info)
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return MemoryPackSerializer.Deserialize<SForwardFlowResponseInfo>(resp.Data.Span);
            }
            return new SForwardFlowResponseInfo();
        }

        [ClientApiAccessAttribute(ClientApiAccess.RelayFlow)]
        public async Task<RelayFlowResponseInfo> GetRelayFlows(ApiControllerParamsInfo param)
        {
            RelayFlowRequestInfo info = param.Content.DeJson<RelayFlowRequestInfo>();
            info.SecretKey = relayClientConfigTransfer.Server.SecretKey;

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)FlowMessengerIds.Relay,
                Payload = MemoryPackSerializer.Serialize(info)
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return MemoryPackSerializer.Deserialize<RelayFlowResponseInfo>(resp.Data.Span);
            }
            return new RelayFlowResponseInfo();
        }
    }

}
