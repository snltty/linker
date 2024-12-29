using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.access
{
    public sealed class AccessServerMessenger : IMessenger
    {
        private readonly IMessengerSender sender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;
        public AccessServerMessenger(IMessengerSender sender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.sender = sender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }

        [MessengerId((ushort)AccessMessengerIds.AccessUpdateForward)]
        public void AccessUpdateForward(IConnection connection)
        {
            AccessUpdateInfo info = serializer.Deserialize<AccessUpdateInfo>(connection.ReceiveRequestWrap.Payload.Span);
            info.FromMachineId = connection.Id;
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && signCaching.TryGet(info.ToMachineId, out SignCacheInfo cache1) && cache1.GroupId == cache.GroupId)
            {
                uint requiestid = connection.ReceiveRequestWrap.RequestId;

                sender.SendReply(new MessageRequestWrap
                {
                    Connection = cache1.Connection,
                    MessengerId = (ushort)AccessMessengerIds.AccessUpdate,
                    Payload = serializer.Serialize(info),
                    Timeout = 3000,
                }).ContinueWith(async (result) =>
                {
                    await sender.ReplyOnly(new MessageResponseWrap
                    {
                        RequestId = requiestid,
                        Connection = connection,
                        Payload = result.Result.Data
                    }, (ushort)AccessMessengerIds.AccessUpdateForward).ConfigureAwait(false);
                });
            }
        }

    }

    public sealed class AccessClientMessenger : IMessenger
    {
        private readonly IAccessStore accessStore;
        private readonly ISerializer serializer;
        public AccessClientMessenger(IAccessStore accessStore, ISerializer serializer)
        {
            this.accessStore = accessStore;
            this.serializer = serializer;
        }
        [MessengerId((ushort)AccessMessengerIds.AccessUpdate)]
        public void AccessUpdate(IConnection connection)
        {
            AccessUpdateInfo info = serializer.Deserialize<AccessUpdateInfo>(connection.ReceiveRequestWrap.Payload.Span);
            accessStore.SetAccess(info);
            connection.Write(Helper.TrueArray);
        }
    }
}
