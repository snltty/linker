using linker.libs;
using linker.messenger.signin;
using static linker.messenger.pcp.PcpTransfer;
using linker.libs.extends;

namespace linker.messenger.pcp
{
    public sealed class PcpClientMessenger : IMessenger
    {
        private readonly ISerializer serializer;
        private readonly IPcpStore pcpStore;
        private readonly PcpTransfer transfer;

        public PcpClientMessenger(ISerializer serializer, IPcpStore pcpStore, PcpTransfer transfer)
        {
            this.serializer = serializer;
            this.pcpStore = pcpStore;
            this.transfer = transfer;
        }

        [MessengerId((ushort)PcpMessengerIds.Begin)]
        public void Begin(IConnection connection)
        {
            _ = transfer.Begin(serializer.Deserialize<Dictionary<string,string>>(connection.ReceiveRequestWrap.Payload.Span));
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)PcpMessengerIds.Fail)]
        public void Fail(IConnection connection)
        {
            _ = transfer.Fail(serializer.Deserialize<Dictionary<string, string>>(connection.ReceiveRequestWrap.Payload.Span));
        }

        [MessengerId((ushort)PcpMessengerIds.Success)]
        public void Success(IConnection connection)
        {
            _ = transfer.Success(serializer.Deserialize<Dictionary<string, string>>(connection.ReceiveRequestWrap.Payload.Span));
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
            Dictionary<string, string> configures = serializer.Deserialize<Dictionary<string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTagInfo info = configures["pcp"].DeJson<TunnelTagInfo>();

            if (signCaching.TryGet(connection.Id, info.ToMachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)PcpMessengerIds.Begin,
                    Payload = serializer.Serialize(configures)
                }).ConfigureAwait(false);
                connection.Write(Helper.TrueArray);
            }
        }


        [MessengerId((ushort)PcpMessengerIds.FailForward)]
        public async Task FailForward(IConnection connection)
        {
            Dictionary<string, string> configures = serializer.Deserialize<Dictionary<string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTagInfo info = configures["pcp"].DeJson<TunnelTagInfo>();

            if (signCaching.TryGet(connection.Id, info.ToMachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)PcpMessengerIds.Fail,
                    Payload = serializer.Serialize(configures)
                }).ConfigureAwait(false);
            }
        }


        [MessengerId((ushort)PcpMessengerIds.SuccessForward)]
        public async Task SuccessForward(IConnection connection)
        {
            Dictionary<string, string> configures = serializer.Deserialize<Dictionary<string, string>>(connection.ReceiveRequestWrap.Payload.Span);
            TunnelTagInfo info = configures["pcp"].DeJson<TunnelTagInfo>();
            if (signCaching.TryGet(connection.Id, info.ToMachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)PcpMessengerIds.Success,
                    Payload = serializer.Serialize(configures)
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
