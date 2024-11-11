using linker.config;
using linker.libs;
using linker.libs.extends;
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
            try
            {
                UpdaterConfirmInfo confirm = MemoryPackSerializer.Deserialize<UpdaterConfirmInfo>(connection.ReceiveRequestWrap.Payload.Span);
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Debug(confirm.ToJson());
                }
                updaterTransfer.Confirm(confirm.Version);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
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


        /// <summary>
        /// 订阅更新消息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.Subscribe)]
        public void Subscribe(IConnection connection)
        {
            string machineId = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            updaterTransfer.Subscribe(machineId);
        }

        /// <summary>
        /// 检查更新
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.Check)]
        public void Check(IConnection connection)
        {
            updaterTransfer.Check();
        }
    }


    public sealed class UpdaterServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly UpdaterServerTransfer updaterServerTransfer;
        private readonly FileConfig fileConfig;

        public UpdaterServerMessenger(IMessengerSender messengerSender, SignCaching signCaching, UpdaterServerTransfer updaterServerTransfer, FileConfig fileConfig)
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
                connection.Write(Helper.FalseArray);
                return;
            }

            //需要密钥
            if ((confirm.All || confirm.GroupAll) && fileConfig.Data.Server.Updater.SecretKey != confirm.SecretKey)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            IEnumerable<SignCacheInfo> machines = new List<SignCacheInfo>();
            //本服务器所有
            if (confirm.All) machines = signCaching.Get().Where(c => c.MachineId != connection.Id);
            //本组所有
            else if (confirm.GroupAll) machines = signCaching.Get(cache.GroupId).Where(c => c.MachineId != connection.Id);
            //某一个
            else machines = signCaching.Get(cache.GroupId).Where(c => c.MachineId == confirm.MachineId && c.GroupId == cache.GroupId);

            UpdaterConfirmV149Info v149 = new UpdaterConfirmV149Info { All = confirm.All, MachineId = confirm.MachineId, Version = confirm.Version };
            confirm.SecretKey = string.Empty;
            byte[] payload = MemoryPackSerializer.Serialize(confirm);
            byte[] payloadV149 = MemoryPackSerializer.Serialize(v149);
            var tasks = machines.Select(c =>
            {
                return messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = c.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.Confirm,
                    Payload = c.Version == "v1.4.9" ? payloadV149 : payload
                });
            }).ToList();

            await Task.WhenAll(tasks);

            connection.Write(Helper.TrueArray);
        }

        /// <summary>
        /// 转发更新消息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.UpdateForward)]
        public void UpdateForward(IConnection connection)
        {
            UpdateClientInfo info = MemoryPackSerializer.Deserialize<UpdateClientInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                byte[] payload = MemoryPackSerializer.Serialize(info.Info);
                foreach (var item in signCaching.Get(cache.GroupId).Where(c => info.ToMachines.Contains(c.MachineId)).Where(c => c.Connected && c.MachineId != connection.Id))
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)UpdaterMessengerIds.Update,
                        Payload = payload
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



        /// <summary>
        /// 订阅更新信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.SubscribeForward)]
        public void SubscribeForward(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                byte[] mechineId = MemoryPackSerializer.Serialize(connection.Id);
                foreach (var item in signCaching.Get(cache.GroupId).Where(c => c.Connected && c.MachineId != connection.Id))
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)UpdaterMessengerIds.Subscribe,
                        Payload = mechineId
                    });
                }

            }
        }

        /// <summary>
        /// 检查更新转发
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.CheckForward)]
        public void CheckForward(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                string toMachineId = string.Empty;
                if (connection.ReceiveRequestWrap.Payload.Length > 0)
                {
                    toMachineId = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
                }

                var clients = string.IsNullOrWhiteSpace(toMachineId)
                    ? signCaching.Get(cache.GroupId).Where(c => c.Connected && c.MachineId != connection.Id)
                    : signCaching.Get(cache.GroupId).Where(c => c.Connected && c.MachineId == toMachineId && c.MachineId != connection.Id);

                foreach (var item in clients)
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)UpdaterMessengerIds.Check,
                    });
                }
            }
        }
    }
}
