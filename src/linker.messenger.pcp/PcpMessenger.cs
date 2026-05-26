using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.pcp
{
    public sealed class PcpClientMessenger : IMessenger
    {
        private readonly ISerializer serializer;
        private readonly IPcpStore pcpStore;

        public PcpClientMessenger(ISerializer serializer, IPcpStore pcpStore)
        {
            this.serializer = serializer;
            this.pcpStore = pcpStore;
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
