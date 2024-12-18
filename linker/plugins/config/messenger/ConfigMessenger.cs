using linker.messenger;
using linker.messenger.signin;
using MemoryPack;

namespace linker.plugins.config.messenger
{
    public sealed class ConfigServerMessenger : IMessenger
    {
        private readonly IMessengerSender sender;
        private readonly SignCaching signCaching;

        public ConfigServerMessenger(IMessengerSender sender, SignCaching signCaching)
        {
            this.sender = sender;
            this.signCaching = signCaching;
        }


        [MessengerId((ushort)ConfigMessengerIds.SyncForward)]
        public async Task SyncForward(IConnection connection)
        {
            ConfigAsyncInfo info = MemoryPackSerializer.Deserialize<ConfigAsyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);
                List<Task> tasks = new List<Task>();
                foreach (SignCacheInfo item in caches.Where(c => c.MachineId != connection.Id && c.Connected))
                {
                    tasks.Add(sender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)ConfigMessengerIds.Sync,
                        Payload = connection.ReceiveRequestWrap.Payload,
                        Timeout = 1000,
                    }));
                }

                await Task.WhenAll(tasks);
            }
        }
    }

    public sealed class ConfigClientMessenger : IMessenger
    {
        private readonly ConfigSyncTreansfer syncTreansfer;

        public ConfigClientMessenger(ConfigSyncTreansfer syncTreansfer)
        {
            this.syncTreansfer = syncTreansfer;
        }

        [MessengerId((ushort)ConfigMessengerIds.Sync)]
        public void Sync(IConnection connection)
        {
            ConfigAsyncInfo info = MemoryPackSerializer.Deserialize<ConfigAsyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
            syncTreansfer.Sync(info);
        }

    }
}
