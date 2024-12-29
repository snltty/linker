using linker.libs.api;
using linker.config;
using linker.serializer;
using linker.client.config;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.flow.messenger;
using linker.libs.extends;
using linker.plugins.relay.client;
using linker.plugins.sforward;
using linker.messenger;
using linker.messenger.signin;

namespace linker.plugins.flow
{
    public sealed class FlowClientApiController : IApiClientController
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly FileConfig config;
        private readonly RunningConfig runningConfig;
        private readonly RelayClientConfigTransfer relayClientConfigTransfer;
        private readonly SForwardTransfer sForwardTransfer;

        public FlowClientApiController(IMessengerSender messengerSender, SignInClientState signInClientState, FileConfig config, RunningConfig runningConfig, RelayClientConfigTransfer relayClientConfigTransfer, SForwardTransfer sForwardTransfer)
        {
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.config = config;
            this.runningConfig = runningConfig;
            this.relayClientConfigTransfer = relayClientConfigTransfer;
            this.sForwardTransfer = sForwardTransfer;
        }

        public async Task<FlowInfo> GetFlows(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)FlowMessengerIds.List,
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return Serializer.Deserialize<FlowInfo>(resp.Data.Span);
            }
            return new FlowInfo();
        }
        public async Task<Dictionary<ushort, FlowItemInfo>> GetMessengerFlows(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)FlowMessengerIds.Messenger,
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return Serializer.Deserialize<Dictionary<ushort, FlowItemInfo>>(resp.Data.Span);
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
                Connection = signInClientState.Connection,
                MessengerId = (ushort)FlowMessengerIds.SForward,
                Payload = Serializer.Serialize(info)
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return Serializer.Deserialize<SForwardFlowResponseInfo>(resp.Data.Span);
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
                Connection = signInClientState.Connection,
                MessengerId = (ushort)FlowMessengerIds.Relay,
                Payload = Serializer.Serialize(info)
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return Serializer.Deserialize<RelayFlowResponseInfo>(resp.Data.Span);
            }
            return new RelayFlowResponseInfo();
        }
    }

}
