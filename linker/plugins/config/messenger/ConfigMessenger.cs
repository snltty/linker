using linker.client.config;
using linker.config;
using linker.libs;
using linker.plugins.messenger;
using linker.plugins.signin.messenger;
using MemoryPack;

namespace linker.plugins.config.messenger
{
    public sealed class ConfigServerMessenger : IMessenger
    {
        private readonly MessengerSender sender;
        private readonly SignCaching signCaching;

        public ConfigServerMessenger(MessengerSender sender, SignCaching signCaching)
        {
            this.sender = sender;
            this.signCaching = signCaching;
        }

        [MessengerId((ushort)ConfigMessengerIds.AccessForward)]
        public void AccessForward(IConnection connection)
        {
            ConfigAccessInfo info = MemoryPackSerializer.Deserialize<ConfigAccessInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                uint requiestid = connection.ReceiveRequestWrap.RequestId;

                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);
                List<Task<MessageResponeInfo>> tasks = new List<Task<MessageResponeInfo>>();
                foreach (SignCacheInfo item in caches.Where(c => c.MachineId != connection.Id && c.Connected))
                {
                    tasks.Add(sender.SendReply(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)ConfigMessengerIds.Access,
                        Payload = connection.ReceiveRequestWrap.Payload,
                        Timeout = 1000,
                    }));
                }

                Task.WhenAll(tasks).ContinueWith(async (result) =>
                {
                    List<ConfigAccessInfo> results = tasks.Where(c => c.Result.Code == MessageResponeCodes.OK)
                    .Select(c => MemoryPackSerializer.Deserialize<ConfigAccessInfo>(c.Result.Data.Span)).ToList();

                    await sender.ReplyOnly(new MessageResponseWrap
                    {
                        RequestId = requiestid,
                        Connection = connection,
                        Payload = MemoryPackSerializer.Serialize(results)
                    }).ConfigureAwait(false);
                });
            }
        }

        [MessengerId((ushort)ConfigMessengerIds.AccessUpdateForward)]
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
                    MessengerId = (ushort)ConfigMessengerIds.AccessUpdate,
                    Payload = MemoryPackSerializer.Serialize(info),
                    Timeout = 3000,
                }).ContinueWith(async (result) =>
                {
                    await sender.ReplyOnly(new MessageResponseWrap
                    {
                        RequestId = requiestid,
                        Connection = connection,
                        Payload = result.Result.Data
                    }).ConfigureAwait(false);
                });
            }
        }

    }

    public sealed class ConfigClientMessenger : IMessenger
    {
        private readonly AccessTransfer accessTransfer;

        public ConfigClientMessenger(AccessTransfer accessTransfer)
        {
            this.accessTransfer = accessTransfer;
        }

        [MessengerId((ushort)ConfigMessengerIds.Access)]
        public void Access(IConnection connection)
        {
            ConfigAccessInfo info = MemoryPackSerializer.Deserialize<ConfigAccessInfo>(connection.ReceiveRequestWrap.Payload.Span);
            accessTransfer.SetAccess(info);
            connection.Write(MemoryPackSerializer.Serialize((accessTransfer.GetAccess())));
        }

        [MessengerId((ushort)ConfigMessengerIds.AccessUpdate)]
        public void AccessUpdate(IConnection connection)
        {
            ConfigUpdateAccessInfo info = MemoryPackSerializer.Deserialize<ConfigUpdateAccessInfo>(connection.ReceiveRequestWrap.Payload.Span);
            accessTransfer.SetAccess(info);
            connection.Write(Helper.TrueArray);
        }
    }
}
