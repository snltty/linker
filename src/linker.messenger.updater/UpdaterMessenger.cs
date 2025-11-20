using linker.libs;
using linker.libs.extends;
using linker.messenger.signin;

namespace linker.messenger.updater
{
    public sealed class UpdaterClientMessenger : IMessenger
    {
        private readonly UpdaterClientTransfer updaterTransfer;
        private readonly ISerializer serializer;

        public UpdaterClientMessenger(UpdaterClientTransfer updaterTransfer, ISerializer serializer)
        {
            this.updaterTransfer = updaterTransfer;
            this.serializer = serializer;
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
                UpdaterConfirmInfo confirm = serializer.Deserialize<UpdaterConfirmInfo>(connection.ReceiveRequestWrap.Payload.Span);
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
            UpdaterInfo info = serializer.Deserialize<UpdaterInfo>(connection.ReceiveRequestWrap.Payload.Span);
            updaterTransfer.Update(info);
        }
        [MessengerId((ushort)UpdaterMessengerIds.Update170)]
        public void Update170(IConnection connection)
        {
            UpdaterInfo170 info = serializer.Deserialize<UpdaterInfo170>(connection.ReceiveRequestWrap.Payload.Span);
            updaterTransfer.Update(info);
        }
        /// <summary>
        /// 关闭信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.Exit)]
        public void Exit(IConnection connection)
        {
            Helper.AppExit(1);
        }


        /// <summary>
        /// 订阅更新消息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.Subscribe)]
        public void Subscribe(IConnection connection)
        {
            string machineId = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
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
        private readonly SignInServerCaching signCaching;
        private readonly UpdaterServerTransfer updaterServerTransfer;
        private readonly ISerializer serializer;
        private readonly IUpdaterServerStore updaterServerStore;

        public UpdaterServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, UpdaterServerTransfer updaterServerTransfer, ISerializer serializer, IUpdaterServerStore updaterServerStore)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.updaterServerTransfer = updaterServerTransfer;
            this.serializer = serializer;
            this.updaterServerStore = updaterServerStore;
        }
        /// <summary>
        /// 获取服务器的更新信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.UpdateServer)]
        public void UpdateServer(IConnection connection)
        {
            var info = updaterServerTransfer.Get();
            UpdaterInfo result = new UpdaterInfo
            {
                MachineId = string.Empty,
                Current = info.Current,
                Length = info.Length,
                Status = info.Status,
                Version = info.Version
            };
            connection.Write(serializer.Serialize(result));
        }
        [MessengerId((ushort)UpdaterMessengerIds.UpdateServer170)]
        public void UpdateServer170(IConnection connection)
        {
            var info = updaterServerTransfer.Get();
            UpdaterInfo170 result = new UpdaterInfo170
            {
                MachineId = string.Empty,
                Current = info.Current,
                Length = info.Length,
                Status = info.Status,
                Version = info.Version
            };
            connection.Write(serializer.Serialize(result));
        }
        [MessengerId((ushort)UpdaterMessengerIds.UpdateServer184)]
        public void UpdateServer184(IConnection connection)
        {
            var info = updaterServerTransfer.Get();
            UpdaterInfo170 result = new UpdaterInfo170
            {
                MachineId = string.Empty,
                Current = info.Current,
                Length = info.Length,
                Status = info.Status,
                Version = info.Version,
                ServerVersion = VersionHelper.Version
            };
            connection.Write(serializer.Serialize(result));
        }
        [MessengerId((ushort)UpdaterMessengerIds.UpdateServer186)]
        public void UpdateServer186(IConnection connection)
        {
            var info = updaterServerTransfer.Get();
            UpdaterInfo170 result = new UpdaterInfo170
            {
                MachineId = string.Empty,
                Current = info.Current,
                Length = info.Length,
                Status = info.Status,
                Version = info.Version,
                ServerVersion = VersionHelper.Version,
                Sync2Server = updaterServerStore.Sync2Server
            };
            connection.Write(serializer.Serialize(result));
        }

        [MessengerId((ushort)UpdaterMessengerIds.Msg)]
        public void Msg(IConnection connection)
        {
            connection.Write(serializer.Serialize(updaterServerTransfer.Get()));
        }
        /// <summary>
        /// 开始更新服务器
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.ConfirmServer)]
        public void ConfirmServer(IConnection connection)
        {
            UpdaterConfirmServerInfo confirm = serializer.Deserialize<UpdaterConfirmServerInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && cache.Super)
            {
                if (string.IsNullOrWhiteSpace(confirm.Version))
                {
                    confirm.Version = updaterServerTransfer.Get().Version;
                }
                if (string.IsNullOrWhiteSpace(confirm.Version))
                {
                    return;
                }
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
            UpdaterConfirmServerInfo confirm = serializer.Deserialize<UpdaterConfirmServerInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && cache.Super)
            {
                Helper.AppExit(1);
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
            UpdaterConfirmInfo confirm = serializer.Deserialize<UpdaterConfirmInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            //需要密钥
            if (confirm.All && cache.Super == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            IEnumerable<SignCacheInfo> machines = new List<SignCacheInfo>();
            //本服务器所有
            if (confirm.All) machines = signCaching.Get().Where(c => c.MachineId != connection.Id);
            //本组所有
            else if (confirm.GroupAll) machines = signCaching.Get(cache).Where(c => c.MachineId != connection.Id);
            //某一个
            else machines = signCaching.Get(cache).Where(c => c.MachineId == confirm.MachineId);

            if (string.IsNullOrWhiteSpace(confirm.Version))
            {
                confirm.Version = updaterServerTransfer.Get().Version;
            }
            if (string.IsNullOrWhiteSpace(confirm.Version))
            {
                return;
            }
            var tasks = machines.Select(c =>
            {
                return messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = c.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.Confirm,
                    Payload = serializer.Serialize(confirm)
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
            UpdaterClientInfo info = serializer.Deserialize<UpdaterClientInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                byte[] payload = serializer.Serialize(info.Info);
                foreach (var item in signCaching.Get(cache).Where(c => info.ToMachines.Contains(c.MachineId)).Where(c => c.Connected && c.MachineId != connection.Id))
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
        [MessengerId((ushort)UpdaterMessengerIds.UpdateForward170)]
        public void UpdateForward170(IConnection connection)
        {
            UpdaterClientInfo170 info = serializer.Deserialize<UpdaterClientInfo170>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                byte[] payload = serializer.Serialize(info.Info);
                foreach (var item in signCaching.Get(cache).Where(c => info.ToMachines.Contains(c.MachineId)).Where(c => c.Connected && c.MachineId != connection.Id))
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)UpdaterMessengerIds.Update170,
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
            string machineId = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.Exit
                }).ConfigureAwait(false);
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
                byte[] mechineId = serializer.Serialize(connection.Id);
                foreach (var item in signCaching.Get(cache).Where(c => c.Connected && c.MachineId != connection.Id))
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
                    toMachineId = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
                }

                var clients = string.IsNullOrWhiteSpace(toMachineId)
                    ? signCaching.Get(cache).Where(c => c.Connected && c.MachineId != connection.Id)
                    : signCaching.Get(cache).Where(c => c.Connected && c.MachineId == toMachineId && c.MachineId != connection.Id);

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
