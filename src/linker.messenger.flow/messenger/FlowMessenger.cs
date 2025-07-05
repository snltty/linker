using linker.libs;
using linker.messenger.relay.server;
using linker.messenger.sforward.server;
using linker.messenger.signin;
using System.Reflection.PortableExecutable;

namespace linker.messenger.flow.messenger
{
    public sealed class FlowMessenger : IMessenger
    {
        private readonly FlowTransfer flowTransfer;
        private readonly flow.FlowMessenger messengerFlow;
        private readonly FlowSForward sForwardFlow;
        private readonly FlowRelay relayFlow;
        private readonly SignInServerCaching signCaching;
        private readonly IRelayServerStore relayServerStore;
        private readonly ISForwardServerStore sForwardServerStore;
        private readonly ISerializer serializer;
        private readonly FlowResolver flowResolver;
        private readonly IMessengerSender messengerSender;

        private DateTime start = DateTime.Now;

        public FlowMessenger(FlowTransfer flowTransfer, flow.FlowMessenger messengerFlow, FlowSForward sForwardFlow, FlowRelay relayFlow, SignInServerCaching signCaching, IRelayServerStore relayServerStore, ISForwardServerStore sForwardServerStore, ISerializer serializer, FlowResolver flowResolver, IMessengerSender messengerSender)
        {
            this.flowTransfer = flowTransfer;
            this.messengerFlow = messengerFlow;
            this.sForwardFlow = sForwardFlow;
            this.relayFlow = relayFlow;
            this.signCaching = signCaching;
            this.relayServerStore = relayServerStore;
            this.sForwardServerStore = sForwardServerStore;
            this.serializer = serializer;
            this.flowResolver = flowResolver;
            this.messengerSender = messengerSender;
        }

        [MessengerId((ushort)FlowMessengerIds.List)]
        public void List(IConnection connection)
        {
            Dictionary<string, FlowItemInfo> dic = flowTransfer.GetFlows();

            signCaching.GetOnline(out int all, out int online);
            dic.TryAdd("_", new FlowItemInfo { FlowName = "_", ReceiveBytes = all, SendtBytes = online });

            FlowInfo serverFlowInfo = new FlowInfo
            {
                Items = dic,
                Start = start,
                Now = DateTime.Now,
            };
            connection.Write(serializer.Serialize(serverFlowInfo));
        }
        [MessengerId((ushort)FlowMessengerIds.Citys)]
        public void Citys(IConnection connection)
        {
            connection.Write(serializer.Serialize(flowResolver.GetCitys()));
        }

        [MessengerId((ushort)FlowMessengerIds.Messenger)]
        public void Messenger(IConnection connection)
        {
            connection.Write(serializer.Serialize(messengerFlow.GetFlows()));
        }
        [MessengerId((ushort)FlowMessengerIds.StopwatchServer)]
        public void StopwatchServer(IConnection connection)
        {
            connection.Write(serializer.Serialize(messengerFlow.GetStopwatch()));
        }

        [MessengerId((ushort)FlowMessengerIds.SForward)]
        public void SForward(IConnection connection)
        {
            sForwardFlow.Update();
            SForwardFlowRequestInfo info = serializer.Deserialize<SForwardFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new SForwardFlowResponseInfo { Count = 0, Data = [], PageSize = info.PageSize, Page = info.Page }));
                return;
            }

            if (cache.Super)
            {
                info.GroupId = string.Empty;
            }
            else
            {
                info.GroupId = cache.GroupId;
            }

            connection.Write(serializer.Serialize(sForwardFlow.GetFlows(info)));
        }

        [MessengerId((ushort)FlowMessengerIds.Relay)]
        public void Relay(IConnection connection)
        {
            relayFlow.Update();
            RelayFlowRequestInfo info = serializer.Deserialize<RelayFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new RelayFlowResponseInfo { Count = 0, Data = [], PageSize = info.PageSize, Page = info.Page }));
                return;
            }

            if (cache.Super)
            {
                info.GroupId = string.Empty;
            }
            else
            {
                info.GroupId = cache.GroupId;
            }

            connection.Write(serializer.Serialize(relayFlow.GetFlows(info)));
        }


        [MessengerId((ushort)FlowMessengerIds.StopwatchForward)]
        public void StopwatchForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)FlowMessengerIds.Stopwatch
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)FlowMessengerIds.StopwatchForward).ConfigureAwait(false);
                    }
                });
            }
        }
    }


    public sealed class FlowClientMessenger : IMessenger
    {
        private readonly flow.FlowMessenger messengerFlow;
        private readonly ISerializer serializer;

        private DateTime start = DateTime.Now;

        public FlowClientMessenger(flow.FlowMessenger messengerFlow, ISerializer serializer)
        {
            this.messengerFlow = messengerFlow;
            this.serializer = serializer;
        }

        [MessengerId((ushort)FlowMessengerIds.Stopwatch)]
        public void Stopwatch(IConnection connection)
        {
            connection.Write(serializer.Serialize(messengerFlow.GetStopwatch()));
        }
    }
}
