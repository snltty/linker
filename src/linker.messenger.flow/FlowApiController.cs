using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.api;
using linker.messenger.flow.messenger;
using linker.messenger.signin;

namespace linker.messenger.flow
{
    public sealed class FlowApiController : IApiController
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly ISerializer serializer;
        private readonly ISignInClientStore signInClientStore;
        private readonly FlowMessenger messengerFlow;
        private readonly FlowTransfer flowTransfer;
        private readonly FlowSForward sForwardFlow;
        private readonly FlowForward forwardFlow;
        private readonly FlowSocks5 socks5Flow;
        private readonly FlowTunnel tunnelFlow;

        private DateTime start = DateTime.Now;

        public FlowApiController(IMessengerSender messengerSender, SignInClientState signInClientState, ISerializer serializer, ISignInClientStore signInClientStore, FlowMessenger messengerFlow, FlowTransfer flowTransfer, FlowSForward sForwardFlow, FlowForward forwardFlow, FlowSocks5 socks5Flow, FlowTunnel tunnelFlow)
        {
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.serializer = serializer;
            this.signInClientStore = signInClientStore;
            this.messengerFlow = messengerFlow;
            this.flowTransfer = flowTransfer;
            this.sForwardFlow = sForwardFlow;
            this.forwardFlow = forwardFlow;
            this.socks5Flow = socks5Flow;
            this.tunnelFlow = tunnelFlow;
        }

        public async Task<FlowInfo> GetFlows(ApiControllerParamsInfo param)
        {
            ushort messengerId = string.IsNullOrWhiteSpace(param.Content) ? (ushort)FlowMessengerIds.List : (ushort)FlowMessengerIds.ListForward;
            if (param.Content == signInClientStore.Id)
            {
                Dictionary<string, FlowItemInfo> dic = flowTransfer.GetFlows();
                return new FlowInfo
                {
                    Items = dic,
                    Start = start,
                    Now = DateTime.Now,
                };
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = messengerId,
                 Payload = serializer.Serialize(param.Content)
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
            ushort messengerId = string.IsNullOrWhiteSpace(param.Content) ? (ushort)FlowMessengerIds.Messenger : (ushort)FlowMessengerIds.MessengerForward;
            if (param.Content == signInClientStore.Id)
            {
                return messengerFlow.GetFlows();
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = messengerId,
                 Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<Dictionary<ushort, FlowItemInfo>>(resp.Data.Span);
            }
            return [];
        }
        public async Task<Dictionary<ushort, FlowItemInfo>> GetStopwatch(ApiControllerParamsInfo param)
        {
            ushort messengerId = string.IsNullOrWhiteSpace(param.Content) ? (ushort)FlowMessengerIds.StopwatchServer : (ushort)FlowMessengerIds.StopwatchForward;
            if (param.Content == signInClientStore.Id)
            {
                return messengerFlow.GetStopwatch();
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = messengerId,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<Dictionary<ushort, FlowItemInfo>>(resp.Data.Span);
            }
            return [];
        }

        [Access(AccessValue.SForwardFlow)]
        public async Task<SForwardFlowResponseInfo> GetSForwardFlows(ApiControllerParamsInfo param)
        {
            SForwardFlowRequestInfo info = param.Content.DeJson<SForwardFlowRequestInfo>();
            ushort messengerId = string.IsNullOrWhiteSpace(info.MachineId) ? (ushort)FlowMessengerIds.SForward : (ushort)FlowMessengerIds.SForwardForward;
            if (info.MachineId == signInClientStore.Id)
            {
                return sForwardFlow.GetFlows(info);
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = messengerId,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<SForwardFlowResponseInfo>(resp.Data.Span);
            }
            return new SForwardFlowResponseInfo();
        }

        [Access(AccessValue.ForwardFlow)]
        public async Task<ForwardFlowResponseInfo> GetForwardFlows(ApiControllerParamsInfo param)
        {
            ForwardFlowRequestInfo info = param.Content.DeJson<ForwardFlowRequestInfo>();
            ushort messengerId = string.IsNullOrWhiteSpace(info.MachineId) ? (ushort)FlowMessengerIds.Forward : (ushort)FlowMessengerIds.ForwardForward;
            if (info.MachineId == signInClientStore.Id)
            {
                return forwardFlow.GetFlows(info);
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = messengerId,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<ForwardFlowResponseInfo>(resp.Data.Span);
            }
            return new ForwardFlowResponseInfo();
        }

        [Access(AccessValue.RelayFlow)]
        public async Task<RelayFlowResponseInfo> GetRelayFlows(ApiControllerParamsInfo param)
        {
            RelayFlowRequestInfo info = param.Content.DeJson<RelayFlowRequestInfo>();

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


        [Access(AccessValue.Socks5Flow)]
        public async Task<Socks5FlowResponseInfo> GetSocks5Flows(ApiControllerParamsInfo param)
        {
            Socks5FlowRequestInfo info = param.Content.DeJson<Socks5FlowRequestInfo>();
            ushort messengerId = string.IsNullOrWhiteSpace(info.MachineId) ? (ushort)FlowMessengerIds.Socks5 : (ushort)FlowMessengerIds.Socks5Forward;
            if (info.MachineId == signInClientStore.Id)
            {
                return socks5Flow.GetFlows(info);
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = messengerId,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<Socks5FlowResponseInfo>(resp.Data.Span);
            }
            return new Socks5FlowResponseInfo();
        }

        [Access(AccessValue.TunnelFlow)]
        public async Task<TunnelFlowResponseInfo> GetTunnelFlows(ApiControllerParamsInfo param)
        {
            TunnelFlowRequestInfo info = param.Content.DeJson<TunnelFlowRequestInfo>();
            ushort messengerId = string.IsNullOrWhiteSpace(info.MachineId) ? (ushort)FlowMessengerIds.Tunnel : (ushort)FlowMessengerIds.TunnelForward;
            if (info.MachineId == signInClientStore.Id)
            {
                return tunnelFlow.GetFlows(info);
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = messengerId,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<TunnelFlowResponseInfo>(resp.Data.Span);
            }
            return new TunnelFlowResponseInfo();
        }
    }

}
