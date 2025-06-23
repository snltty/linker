using linker.tunnel;
using linker.tunnel.transport;
using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.pcp
{
    public sealed class PcpClientMessenger : IMessenger
    {
        private readonly TunnelTransfer tunnel;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly IPcpStore pcpStore;

        public PcpClientMessenger(TunnelTransfer tunnel, IMessengerSender messengerSender, ISerializer serializer, IPcpStore pcpStore)
        {
            this.tunnel = tunnel;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.pcpStore = pcpStore;
        }

        [MessengerId((ushort)PcpMessengerIds.Begin)]
        public void Begin(IConnection connection)
        {
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)PcpMessengerIds.Fail)]
        public void Fail(IConnection connection)
        {
        }

        [MessengerId((ushort)PcpMessengerIds.Success)]
        public void Success(IConnection connection)
        {
        }

        [MessengerId((ushort)PcpMessengerIds.Nodes)]
        public void Nodes(IConnection connection)
        {
            connection.Write(serializer.Serialize(pcpStore.PcpHistory.History));
        }
    }

    public sealed class PcpServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;
        public PcpServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }

        [MessengerId((ushort)PcpMessengerIds.BeginForward)]
        public async Task BeginForward(IConnection connection)
        {
            TunnelTransportInfo info = serializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, info.Remote.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                info.Local.MachineName = from.MachineName;
                info.Remote.MachineName = to.MachineName;

                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)PcpMessengerIds.Begin,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
        }


        [MessengerId((ushort)PcpMessengerIds.FailForward)]
        public async Task FailForward(IConnection connection)
        {
            TunnelTransportInfo info = serializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.Remote.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                info.Local.MachineName = from.MachineName;
                info.Remote.MachineName = to.MachineName;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)PcpMessengerIds.Fail,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
        }


        [MessengerId((ushort)PcpMessengerIds.SuccessForward)]
        public async Task SuccessForward(IConnection connection)
        {
            TunnelTransportInfo info = serializer.Deserialize<TunnelTransportInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.Remote.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                info.Local.MachineName = from.MachineName;
                info.Remote.MachineName = to.MachineName;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)PcpMessengerIds.Success,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
        }

        [MessengerId((ushort)PcpMessengerIds.NodesForward)]
        public void NodesForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)PcpMessengerIds.Nodes,
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)PcpMessengerIds.NodesForward).ConfigureAwait(false);
                    }

                });
            }
        }
    }

}
