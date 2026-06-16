using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.mesh
{
    public sealed class MeshClientMessenger : IMessenger
    {
        private readonly ISerializer serializer;
        private readonly IMeshStore meshStore;

        public MeshClientMessenger(ISerializer serializer, IMeshStore meshStore)
        {
            this.serializer = serializer;
            this.meshStore = meshStore;
        }

        [MessengerId((ushort)MeshMessengerIds.Nodes)]
        public void Nodes(IConnection connection)
        {
            connection.Write(serializer.Serialize(meshStore.MeshHistory.History));
        }
    }

    public sealed class MeshServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;
        public MeshServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }

        [MessengerId((ushort)MeshMessengerIds.NodesForward)]
        public void NodesForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)MeshMessengerIds.Nodes,
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)MeshMessengerIds.NodesForward).ConfigureAwait(false);
                    }
                });
            }
        }
    }
}
