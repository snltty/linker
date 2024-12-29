using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.decenter
{
    public sealed class DecenterServerMessenger : IMessenger
    {
        private readonly IMessengerSender sender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;

        public DecenterServerMessenger(IMessengerSender sender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.sender = sender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }


        [MessengerId((ushort)DecenterMessengerIds.SyncForward)]
        public void SyncForward(IConnection connection)
        {
            DecenterSyncInfo info = serializer.Deserialize<DecenterSyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
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
                            Payload = serializer.Serialize(results)
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
        private readonly DecenterClientTransfer syncTreansfer;
        private readonly ISerializer serializer;

        public DecenterClientMessenger(DecenterClientTransfer syncTreansfer, ISerializer serializer)
        {
            this.syncTreansfer = syncTreansfer;
            this.serializer = serializer;
        }

        [MessengerId((ushort)DecenterMessengerIds.Sync)]
        public void Sync(IConnection connection)
        {
            DecenterSyncInfo info = serializer.Deserialize<DecenterSyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(syncTreansfer.Sync(info));
        }

    }
}
