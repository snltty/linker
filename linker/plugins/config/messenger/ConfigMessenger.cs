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


        [MessengerId((ushort)ConfigMessengerIds.AccessForward)]
        public void AccessForward(IConnection connection)
        {
            string machineId = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && signCaching.TryGet(machineId, out SignCacheInfo cache1) && cache1.GroupId == cache.GroupId)
            {
                uint requiestid = connection.ReceiveRequestWrap.RequestId;

                sender.SendReply(new MessageRequestWrap
                {
                    Connection = cache1.Connection,
                    MessengerId = (ushort)ConfigMessengerIds.Access,
                    Payload = connection.ReceiveRequestWrap.Payload,
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

        [MessengerId((ushort)ConfigMessengerIds.AccessUpdateForward)]
        public void AccessUpdateForward(IConnection connection)
        {
            ConfigUpdateAccessInfo info = MemoryPackSerializer.Deserialize<ConfigUpdateAccessInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && signCaching.TryGet(info.MachineId, out SignCacheInfo cache1) && cache1.GroupId == cache.GroupId)
            {
                uint requiestid = connection.ReceiveRequestWrap.RequestId;

                sender.SendReply(new MessageRequestWrap
                {
                    Connection = cache1.Connection,
                    MessengerId = (ushort)ConfigMessengerIds.AccessUpdate,
                    Payload = connection.ReceiveRequestWrap.Payload,
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
        private readonly RunningConfigTransfer runningConfigTransfer;
        private readonly FileConfig fileConfig;

        public ConfigClientMessenger(RunningConfigTransfer runningConfigTransfer, FileConfig fileConfig)
        {
            this.runningConfigTransfer = runningConfigTransfer;
            this.fileConfig = fileConfig;
        }

        [MessengerId((ushort)ConfigMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            ConfigVersionInfo info = MemoryPackSerializer.Deserialize<ConfigVersionInfo>(connection.ReceiveRequestWrap.Payload.Span);
            Memory<byte> data = runningConfigTransfer.InputConfig(info);
            connection.Write(data);
        }


        [MessengerId((ushort)ConfigMessengerIds.Access)]
        public void Access(IConnection connection)
        {
            connection.Write(MemoryPackSerializer.Serialize(((ulong)fileConfig.Data.Client.Access)));
        }

        [MessengerId((ushort)ConfigMessengerIds.AccessUpdate)]
        public void AccessUpdate(IConnection connection)
        {
            ConfigUpdateAccessInfo info = MemoryPackSerializer.Deserialize<ConfigUpdateAccessInfo>(connection.ReceiveRequestWrap.Payload.Span);

            fileConfig.Data.Client.Access = (ClientApiAccess)info.Access;
            fileConfig.Data.Update();
            connection.Write(Helper.TrueArray);
        }
    }
}
