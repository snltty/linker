using linker.config;
using linker.messenger;
using linker.messenger.signin;
using linker.plugins.relay.server;
using linker.plugins.sforward;
using MemoryPack;

namespace linker.plugins.flow.messenger
{
    public sealed class FlowMessenger : IMessenger
    {
        private readonly FlowTransfer flowTransfer;
        private readonly MessengerFlow messengerFlow;
        private readonly SForwardFlow sForwardFlow;
        private readonly RelayFlow relayFlow;
        private readonly SignCaching signCaching;
        private readonly FileConfig fileConfig;
        private readonly RelayServerConfigTransfer relayServerConfigTransfer;
        private readonly SForwardServerConfigTransfer sForwardServerConfigTransfer;

        private DateTime start = DateTime.Now;

        public FlowMessenger(FlowTransfer flowTransfer, MessengerFlow messengerFlow, SForwardFlow sForwardFlow, RelayFlow relayFlow, SignCaching signCaching, FileConfig fileConfig, RelayServerConfigTransfer relayServerConfigTransfer, SForwardServerConfigTransfer sForwardServerConfigTransfer)
        {
            this.flowTransfer = flowTransfer;
            this.messengerFlow = messengerFlow;
            this.sForwardFlow = sForwardFlow;
            this.relayFlow = relayFlow;
            this.signCaching = signCaching;
            this.fileConfig = fileConfig;
            this.relayServerConfigTransfer = relayServerConfigTransfer;
            this.sForwardServerConfigTransfer = sForwardServerConfigTransfer;
        }

        [MessengerId((ushort)FlowMessengerIds.List)]
        public void List(IConnection connection)
        {
            Dictionary<string, FlowItemInfo> dic = flowTransfer.GetFlows();

            signCaching.GetOnline(out int all, out int online);
            dic.TryAdd("_", new FlowItemInfo { FlowName = "_", ReceiveBytes = (ulong)all, SendtBytes = (ulong)online });

            FlowInfo serverFlowInfo = new FlowInfo
            {
                Items = dic,
                Start = start,
                Now = DateTime.Now,
            };
            connection.Write(MemoryPackSerializer.Serialize(serverFlowInfo));
        }

        [MessengerId((ushort)FlowMessengerIds.Messenger)]
        public void Messenger(IConnection connection)
        {
            connection.Write(MemoryPackSerializer.Serialize(messengerFlow.GetFlows()));
        }

        [MessengerId((ushort)FlowMessengerIds.SForward)]
        public void SForward(IConnection connection)
        {
            sForwardFlow.Update();
            SForwardFlowRequestInfo info = MemoryPackSerializer.Deserialize<SForwardFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (sForwardServerConfigTransfer.SecretKey == info.SecretKey)
            {
                info.GroupId = string.Empty;
            }
            else
            {
                if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
                {
                    info.GroupId = cache.GroupId;
                }
                else
                {
                    info.GroupId = Guid.NewGuid().ToString();
                }
            }

            connection.Write(MemoryPackSerializer.Serialize(sForwardFlow.GetFlows(info)));
        }

        [MessengerId((ushort)FlowMessengerIds.Relay)]
        public void Relay(IConnection connection)
        {
            relayFlow.Update();
            RelayFlowRequestInfo info = MemoryPackSerializer.Deserialize<RelayFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (relayServerConfigTransfer.SecretKey == info.SecretKey)
            {
                info.GroupId = string.Empty;
            }
            else
            {
                if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
                {
                    info.GroupId = cache.GroupId;
                }
                else
                {
                    info.GroupId = Guid.NewGuid().ToString();
                }
            }

            connection.Write(MemoryPackSerializer.Serialize(relayFlow.GetFlows(info)));
        }
    }

}
