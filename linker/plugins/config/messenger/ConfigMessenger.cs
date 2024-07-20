using linker.client.config;
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

        [MessengerId((ushort)ConfigMessengerIds.UpdateForward)]
        public void UpdateForward(IConnection connection)
        {
            ConfigVersionInfo info = MemoryPackSerializer.Deserialize<ConfigVersionInfo>(connection.ReceiveRequestWrap.Payload.Span);
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
                        MessengerId = (ushort)ConfigMessengerIds.Update,
                        Payload = connection.ReceiveRequestWrap.Payload,
                        Timeout = 1000,
                    }));
                }

                Task.WhenAll(tasks).ContinueWith(async (result) =>
                {
                    ConfigVersionInfo _info = tasks.Where(c => c.Result.Code == MessageResponeCodes.OK && c.Result.Data.Length > 0)
                    .Select(c => MemoryPackSerializer.Deserialize<ConfigVersionInfo>(c.Result.Data.Span)).MaxBy(c => c.Version);

                    await sender.ReplyOnly(new MessageResponseWrap
                    {
                        RequestId = requiestid,
                        Connection = connection,
                        Payload = MemoryPackSerializer.Serialize(_info ?? info)
                    }).ConfigureAwait(false);
                });
            }
        }

    }

    public sealed class ConfigClientMessenger : IMessenger
    {
        private readonly RunningConfigTransfer runningConfigTransfer;

        public ConfigClientMessenger(RunningConfigTransfer runningConfigTransfer)
        {
            this.runningConfigTransfer = runningConfigTransfer;
        }

        [MessengerId((ushort)ConfigMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            ConfigVersionInfo info = MemoryPackSerializer.Deserialize<ConfigVersionInfo>(connection.ReceiveRequestWrap.Payload.Span);
            Memory<byte> data = runningConfigTransfer.InputConfig(info);
            connection.Write(data);
        }
    }
}
