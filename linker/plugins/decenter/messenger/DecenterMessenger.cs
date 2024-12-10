using linker.libs;
using linker.plugins.messenger;
using linker.plugins.signin.messenger;
using MemoryPack;

namespace linker.plugins.decenter.messenger
{
    public sealed class DecenterServerMessenger : IMessenger
    {
        private readonly IMessengerSender sender;
        private readonly SignCaching signCaching;

        public DecenterServerMessenger(IMessengerSender sender, SignCaching signCaching)
        {
            this.sender = sender;
            this.signCaching = signCaching;
        }


        [MessengerId((ushort)DecenterMessengerIds.SyncForward)]
        public void SyncForward(IConnection connection)
        {
            DecenterSyncInfo info = MemoryPackSerializer.Deserialize<DecenterSyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
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
                        MessengerId = (ushort)DecenterMessengerIds.Sync,
                        Payload = connection.ReceiveRequestWrap.Payload,
                        Timeout = 5000,
                    }));
                }

                Task.WhenAll(tasks).ContinueWith(async (result) =>
                {
                    try
                    {
                        List<ReadOnlyMemory<byte>> results = tasks.Where(c => c.Result.Code == MessageResponeCodes.OK).Select(c => c.Result.Data).ToList();
                        await sender.ReplyOnly(new MessageResponseWrap
                        {
                            RequestId = requiestid,
                            Connection = connection,
                            Payload = MemoryPackSerializer.Serialize(results)
                        }, (ushort)DecenterMessengerIds.SyncForward).ConfigureAwait(false);

                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                });
            }
        }
    }

    public sealed class DecenterClientMessenger : IMessenger
    {
        private readonly DecenterTransfer syncTreansfer;

        public DecenterClientMessenger(DecenterTransfer syncTreansfer)
        {
            this.syncTreansfer = syncTreansfer;
        }

        [MessengerId((ushort)DecenterMessengerIds.Sync)]
        public void Sync(IConnection connection)
        {
            DecenterSyncInfo info = MemoryPackSerializer.Deserialize<DecenterSyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(syncTreansfer.Sync(info));
        }

    }
}
