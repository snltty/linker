using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.sync
{
    public sealed class SyncServerMessenger : IMessenger
    {
        private readonly IMessengerSender sender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;

        public SyncServerMessenger(IMessengerSender sender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.sender = sender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }


        [MessengerId((ushort)ConfigMessengerIds.SyncForward)]
        public async Task SyncForward(IConnection connection)
        {
            SyncInfo info = serializer.Deserialize<SyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                List<SignCacheInfo> caches = signCaching.Get(cache);
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

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }
        [MessengerId((ushort)ConfigMessengerIds.Sync184Forward)]
        public async Task Sync184Forward(IConnection connection)
        {
            Sync184Info info = serializer.Deserialize<Sync184Info>(connection.ReceiveRequestWrap.Payload.Span);
            string[] ids = info.Ids;
            info.Ids = [];
            Memory<byte> data = serializer.Serialize(info);

            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                List<SignCacheInfo> caches = signCaching.Get(cache);
                List<Task> tasks = [];
                foreach (SignCacheInfo item in caches.Where(c => c.MachineId != connection.Id && c.Connected && (ids.Contains(c.Id) || ids.Length == 0)))
                {
                    // 【安全修复 P1】添加授权检查：防止向无权限的机器推送数据
                    if (cache.Super == false && cache.GroupId != item.GroupId)
                    {
                        continue; // 跳过无权限的目标
                    }

                    tasks.Add(sender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)ConfigMessengerIds.Sync184,
                        Payload = data,
                        Timeout = 1000,
                    }));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }
    }

    public sealed class SyncClientMessenger : IMessenger
    {
        private readonly SyncTreansfer syncTreansfer;
        private readonly ISerializer serializer;
        public SyncClientMessenger(SyncTreansfer syncTreansfer, ISerializer serializer)
        {
            this.syncTreansfer = syncTreansfer;
            this.serializer = serializer;
        }

        [MessengerId((ushort)ConfigMessengerIds.Sync)]
        public void Sync(IConnection connection)
        {
            SyncInfo info = serializer.Deserialize<SyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
            syncTreansfer.Sync(info);
        }

        [MessengerId((ushort)ConfigMessengerIds.Sync184)]
        public void Sync184(IConnection connection)
        {
            Sync184Info info = serializer.Deserialize<Sync184Info>(connection.ReceiveRequestWrap.Payload.Span);
            syncTreansfer.Sync(info);
        }

    }
}
