﻿using linker.client.config;
using linker.config;
using linker.libs;
using linker.libs.extends;
using linker.plugins.client;
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
                    },(ushort)ConfigMessengerIds.AccessForward).ConfigureAwait(false);
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
                    }, (ushort)ConfigMessengerIds.AccessUpdateForward).ConfigureAwait(false);
                });
            }
        }


        [MessengerId((ushort)ConfigMessengerIds.SecretKeyAsyncForward)]
        public async Task SecretKeyAsyncForward(IConnection connection)
        {
            SecretKeyAsyncInfo info = MemoryPackSerializer.Deserialize<SecretKeyAsyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);
                List<Task> tasks = new List<Task>();
                foreach (SignCacheInfo item in caches.Where(c => c.MachineId != connection.Id && c.Connected))
                {
                    tasks.Add(sender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)ConfigMessengerIds.SecretKeyAsync,
                        Payload = connection.ReceiveRequestWrap.Payload,
                        Timeout = 1000,
                    }));
                }

                await Task.WhenAll(tasks);
            }
        }
        [MessengerId((ushort)ConfigMessengerIds.ServerAsyncForward)]
        public async Task ServerAsyncForward(IConnection connection)
        {
            ServerAsyncInfo info = MemoryPackSerializer.Deserialize<ServerAsyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);
                List<Task> tasks = new List<Task>();
                foreach (SignCacheInfo item in caches.Where(c => c.MachineId != connection.Id && c.Connected))
                {
                    tasks.Add(sender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)ConfigMessengerIds.ServerAsync,
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
        private readonly AccessTransfer accessTransfer;
        private readonly FileConfig fileConfig;
        private readonly ClientSignInTransfer clientSignInTransfer;

        public ConfigClientMessenger(AccessTransfer accessTransfer, FileConfig fileConfig, ClientSignInTransfer clientSignInTransfer)
        {
            this.accessTransfer = accessTransfer;
            this.fileConfig = fileConfig;
            this.clientSignInTransfer = clientSignInTransfer;
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

        [MessengerId((ushort)ConfigMessengerIds.SecretKeyAsync)]
        public void SecretKeyAsync(IConnection connection)
        {
            SecretKeyAsyncInfo info = MemoryPackSerializer.Deserialize<SecretKeyAsyncInfo>(connection.ReceiveRequestWrap.Payload.Span);

            foreach (var item in fileConfig.Data.Client.Servers)
            {
                item.SecretKey = info.SignSecretKey;
            }
            foreach (var item in fileConfig.Data.Client.Relay.Servers)
            {
                item.SecretKey = info.RelaySecretKey;
            }
            fileConfig.Data.Client.SForward.SecretKey = info.SForwardSecretKey;

            fileConfig.Data.Update();
            clientSignInTransfer.SignOut();
            _ = clientSignInTransfer.SignIn();
        }

        [MessengerId((ushort)ConfigMessengerIds.ServerAsync)]
        public void ServerAsync(IConnection connection)
        {
            ServerAsyncInfo info = MemoryPackSerializer.Deserialize<ServerAsyncInfo>(connection.ReceiveRequestWrap.Payload.Span);

            fileConfig.Data.Client.Servers = info.SignServers;
            fileConfig.Data.Client.Relay.Servers = info.RelayServers;
            fileConfig.Data.Client.Tunnel.Servers = info.TunnelServers.ToList();

            fileConfig.Data.Update();
            clientSignInTransfer.SignOut();
            _ = clientSignInTransfer.SignIn();
        }
    }
}
