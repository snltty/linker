using linker.libs;
using linker.libs.api;
using linker.messenger.api;
using linker.messenger.signin;
using IApiServer = linker.messenger.api.IApiServer;

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

        [MessengerId((ushort)AccessMessengerIds.SetApiPasswordForward)]
        public void SetApiPasswordForward(IConnection connection)
        {
            ApiPasswordUpdateInfo info = serializer.Deserialize<ApiPasswordUpdateInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && signCaching.TryGet(info.MachineId, out SignCacheInfo cache1) && cache1.GroupId == cache.GroupId)
            {
                sender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache1.Connection,
                    MessengerId = (ushort)AccessMessengerIds.SetApiPassword,
                    Payload = serializer.Serialize(info.Password)
                });
                connection.Write(Helper.TrueArray);
                return;
            }
            connection.Write(Helper.FalseArray);
        }

    }

    public sealed class AccessClientMessenger : IMessenger
    {
        private readonly IAccessStore accessStore;
        private readonly ISerializer serializer;
        private readonly IApiStore apiStore;
        private readonly IApiServer apiServer;
        public AccessClientMessenger(IAccessStore accessStore, ISerializer serializer, IApiStore apiStore, IApiServer apiServer)
        {
            this.accessStore = accessStore;
            this.serializer = serializer;
            this.apiStore = apiStore;
            this.apiServer = apiServer;
        }
        [MessengerId((ushort)AccessMessengerIds.AccessUpdate)]
        public void AccessUpdate(IConnection connection)
        {
            AccessUpdateInfo info = serializer.Deserialize<AccessUpdateInfo>(connection.ReceiveRequestWrap.Payload.Span);
            accessStore.SetAccess(info);
            connection.Write(Helper.TrueArray);
        }
        [MessengerId((ushort)AccessMessengerIds.SetApiPassword)]
        public void SetApiPassword(IConnection connection)
        {
            string password = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            apiStore.SetApiPassword(password);
            apiStore.Confirm();
            apiServer.SetPassword(password);
            connection.Write(Helper.TrueArray);
        }
    }
}
