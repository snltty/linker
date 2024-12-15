using linker.config;
using linker.libs;
using linker.messenger;
using linker.plugins.client;
using linker.plugins.messenger;
using linker.plugins.signin.messenger;
using MemoryPack;

namespace linker.plugins.access.messenger
{
    public sealed class AccessServerMessenger : IMessenger
    {
        private readonly IMessengerSender sender;
        private readonly SignCaching signCaching;

        public AccessServerMessenger(IMessengerSender sender, SignCaching signCaching)
        {
            this.sender = sender;
            this.signCaching = signCaching;
        }

        [MessengerId((ushort)AccessMessengerIds.AccessUpdateForward)]
        public void AccessUpdateForward(IConnection connection)
        {
            ConfigUpdateAccessInfo info = MemoryPackSerializer.Deserialize<ConfigUpdateAccessInfo>(connection.ReceiveRequestWrap.Payload.Span);
            info.FromMachineId = connection.Id;
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && signCaching.TryGet(info.ToMachineId, out SignCacheInfo cache1) && cache1.GroupId == cache.GroupId)
            {
                uint requiestid = connection.ReceiveRequestWrap.RequestId;

                sender.SendReply(new MessageRequestWrap
                {
                    Connection = cache1.Connection,
                    MessengerId = (ushort)AccessMessengerIds.AccessUpdate,
                    Payload = MemoryPackSerializer.Serialize(info),
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
        private readonly AccessTransfer accessTransfer;
        private readonly FileConfig fileConfig;
        private readonly ClientSignInTransfer clientSignInTransfer;

        public AccessClientMessenger(AccessTransfer accessTransfer, FileConfig fileConfig, ClientSignInTransfer clientSignInTransfer)
        {
            this.accessTransfer = accessTransfer;
            this.fileConfig = fileConfig;
            this.clientSignInTransfer = clientSignInTransfer;
        }
        [MessengerId((ushort)AccessMessengerIds.AccessUpdate)]
        public void AccessUpdate(IConnection connection)
        {
            ConfigUpdateAccessInfo info = MemoryPackSerializer.Deserialize<ConfigUpdateAccessInfo>(connection.ReceiveRequestWrap.Payload.Span);
            accessTransfer.SetAccess(info);
            connection.Write(Helper.TrueArray);
        }
    }
}
