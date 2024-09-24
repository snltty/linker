using linker.config;
using linker.plugins.messenger;
using linker.plugins.signin.messenger;
using linker.plugins.updater.config;
using MemoryPack;

namespace linker.plugins.updater.messenger
{
    public sealed class UpdaterClientMessenger : IMessenger
    {
        private readonly UpdaterClientTransfer updaterTransfer;
        public UpdaterClientMessenger(UpdaterClientTransfer updaterTransfer)
        {
            this.updaterTransfer = updaterTransfer;
        }

        /// <summary>
        /// 确认更新
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.Confirm)]
        public void Confirm(IConnection connection)
        {
            UpdaterConfirmInfo confirm = MemoryPackSerializer.Deserialize<UpdaterConfirmInfo>(connection.ReceiveRequestWrap.Payload.Span);
            updaterTransfer.Confirm(confirm.Version);
        }

        /// <summary>
        /// 更新信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            UpdateInfo info = MemoryPackSerializer.Deserialize<UpdateInfo>(connection.ReceiveRequestWrap.Payload.Span);
            updaterTransfer.Update(info);
        }
        /// <summary>
        /// 关闭信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.Exit)]
        public void Exit(IConnection connection)
        {
            Environment.Exit(1);
        }
    }


    public sealed class UpdaterServerMessenger : IMessenger
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly UpdaterServerTransfer updaterServerTransfer;
        private readonly FileConfig fileConfig;

        public UpdaterServerMessenger(MessengerSender messengerSender, SignCaching signCaching, UpdaterServerTransfer updaterServerTransfer, FileConfig fileConfig)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.updaterServerTransfer = updaterServerTransfer;
            this.fileConfig = fileConfig;
        }
        /// <summary>
        /// 获取服务器的更新信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.UpdateServer)]
        public void UpdateServer(IConnection connection)
        {
            connection.Write(MemoryPackSerializer.Serialize(updaterServerTransfer.Get()));
        }
        /// <summary>
        /// 开始更新服务器
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.ConfirmServer)]
        public void ConfirmServer(IConnection connection)
        {
            UpdaterConfirmServerInfo confirm = MemoryPackSerializer.Deserialize<UpdaterConfirmServerInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (fileConfig.Data.Server.Updater.SecretKey == confirm.SecretKey)
            {
                updaterServerTransfer.Confirm(confirm.Version);
            }
        }
        /// <summary>
        /// 关闭服务器
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.ExitServer)]
        public void ExitServer(IConnection connection)
        {
            UpdaterConfirmServerInfo confirm = MemoryPackSerializer.Deserialize<UpdaterConfirmServerInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (fileConfig.Data.Server.Updater.SecretKey == confirm.SecretKey)
            {
                Environment.Exit(1);
            }
        }


        /// <summary>
        /// 转发确认更新消息
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)UpdaterMessengerIds.ConfirmForward)]
        public async Task ConfirmForward(IConnection connection)
        {
            UpdaterConfirmInfo confirm = MemoryPackSerializer.Deserialize<UpdaterConfirmInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                return;
            }

            if (confirm.All)
            {
                var tasks = signCaching.Get(cache.GroupId).Where(c => c.MachineId != connection.Id).Select(c =>
                {
                    return messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = c.Connection,
                        MessengerId = (ushort)UpdaterMessengerIds.Confirm,
                        Payload = connection.ReceiveRequestWrap.Payload
                    });
                });

                await Task.WhenAll(tasks);
            }
            else if (signCaching.TryGet(confirm.MachineId, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache1.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.Confirm,
                    Payload = connection.ReceiveRequestWrap.Payload
                });
            }
        }

        /// <summary>
        /// 转发更新消息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.UpdateForward)]
        public void UpdateForward(IConnection connection)
        {
            UpdateInfo info = MemoryPackSerializer.Deserialize<UpdateInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                foreach (var item in signCaching.Get(cache.GroupId).Where(c => c.Connected && c.MachineId != connection.Id))
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)UpdaterMessengerIds.Update,
                        Payload = connection.ReceiveRequestWrap.Payload
                    });
                }

            }
        }

        /// <summary>
        /// 转发关闭消息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.ExitForward)]
        public async Task ExitForward(IConnection connection)
        {
            string machineId = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && signCaching.TryGet(machineId, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache1.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.Exit
                });
            }
        }
    }
}
