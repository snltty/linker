using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.api;
using linker.messenger.flow.messenger;
using linker.messenger.relay.client;
using linker.messenger.sforward.client;
using linker.messenger.signin;

namespace linker.messenger.flow
{
    public sealed class FlowApiController : IApiController
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly IRelayClientStore relayClientStore;
        private readonly ISForwardClientStore sForwardClientStore;
        private readonly ISerializer serializer;
        private readonly ISignInClientStore signInClientStore;
        private readonly MessengerFlow messengerFlow;

        public FlowApiController(IMessengerSender messengerSender, SignInClientState signInClientState, IRelayClientStore relayClientStore, ISForwardClientStore sForwardClientStore, ISerializer serializer, ISignInClientStore signInClientStore, MessengerFlow messengerFlow)
        {
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.relayClientStore = relayClientStore;
            this.sForwardClientStore = sForwardClientStore;
            this.serializer = serializer;
            this.signInClientStore = signInClientStore;
            this.messengerFlow = messengerFlow;
        }

        public async Task<FlowInfo> GetFlows(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)FlowMessengerIds.List,
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<FlowInfo>(resp.Data.Span);
            }
            return new FlowInfo();
        }
        public async Task<List<FlowReportNetInfo>> GetCitys(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)FlowMessengerIds.Citys,
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<List<FlowReportNetInfo>>(resp.Data.Span);
            }
            return new List<FlowReportNetInfo>();
        }

        public async Task<Dictionary<ushort, FlowItemInfo>> GetMessengerFlows(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)FlowMessengerIds.Messenger,
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<Dictionary<ushort, FlowItemInfo>>(resp.Data.Span);
            }
            return new Dictionary<ushort, FlowItemInfo>();
        }
        public async Task<Dictionary<ushort, FlowItemInfo>> GetStopwatch(ApiControllerParamsInfo param)
        {
            if (string.IsNullOrWhiteSpace(param.Content))
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)FlowMessengerIds.StopwatchServer,
                    Payload = serializer.Serialize(param.Content)
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
                {
                    return serializer.Deserialize<Dictionary<ushort, FlowItemInfo>>(resp.Data.Span);
                }
            }
            else if (param.Content == signInClientStore.Id)
            {
                return messengerFlow.GetStopwatch();
            }
            else
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)FlowMessengerIds.StopwatchForward,
                    Payload = serializer.Serialize(param.Content)
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
                {
                    return serializer.Deserialize<Dictionary<ushort, FlowItemInfo>>(resp.Data.Span);
                }
            }
            return new Dictionary<ushort, FlowItemInfo>();
        }

        [Access(AccessValue.SForwardFlow)]
        public async Task<SForwardFlowResponseInfo> GetSForwardFlows(ApiControllerParamsInfo param)
        {
            SForwardFlowRequestInfo info = param.Content.DeJson<SForwardFlowRequestInfo>();
            info.SecretKey = sForwardClientStore.SecretKey;

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)FlowMessengerIds.SForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<SForwardFlowResponseInfo>(resp.Data.Span);
            }
            return new SForwardFlowResponseInfo();
        }

        [Access(AccessValue.RelayFlow)]
        public async Task<RelayFlowResponseInfo> GetRelayFlows(ApiControllerParamsInfo param)
        {
            RelayFlowRequestInfo info = param.Content.DeJson<RelayFlowRequestInfo>();
            info.SecretKey = relayClientStore.Server.SecretKey;

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)FlowMessengerIds.Relay,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<RelayFlowResponseInfo>(resp.Data.Span);
            }
            return new RelayFlowResponseInfo();
        }
    }

}
