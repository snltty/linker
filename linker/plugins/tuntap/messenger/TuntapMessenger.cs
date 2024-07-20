using linker.plugins.messenger;
using linker.plugins.signin.messenger;
using linker.plugins.tuntap.vea;
using MemoryPack;

namespace linker.plugins.tuntap.messenger
{
    public sealed class TuntapClientMessenger : IMessenger
    {
        private readonly TuntapTransfer tuntapTransfer;
        public TuntapClientMessenger(TuntapTransfer tuntapTransfer)
        {
            this.tuntapTransfer = tuntapTransfer;
        }

        /// <summary>
        /// 运行网卡
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.Run)]
        public void Run(IConnection connection)
        {
            tuntapTransfer.Stop();
            tuntapTransfer.Run();
        }

        /// <summary>
        /// 停止网卡
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.Stop)]
        public void Stop(IConnection connection)
        {
            tuntapTransfer.Stop();
        }

        /// <summary>
        /// 更新网卡信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            TuntapInfo info = MemoryPackSerializer.Deserialize<TuntapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            tuntapTransfer.OnUpdate(info);
        }

        /// <summary>
        /// 收到别人的网卡信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.Config)]
        public void Config(IConnection connection)
        {
            TuntapInfo info = MemoryPackSerializer.Deserialize<TuntapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            TuntapInfo _info = tuntapTransfer.OnConfig(info);
            connection.Write(MemoryPackSerializer.Serialize(_info));
        }
    }


    public sealed class TuntapServerMessenger : IMessenger
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;

        public TuntapServerMessenger(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }

        /// <summary>
        /// 转发运行命令
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TuntapMessengerIds.RunForward)]
        public async Task RunForward(IConnection connection)
        {
            string name = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(name, out SignCacheInfo cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)TuntapMessengerIds.Run
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 转发停止命令
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TuntapMessengerIds.StopForward)]
        public async Task StopForward(IConnection connection)
        {
            string name = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(name, out SignCacheInfo cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)TuntapMessengerIds.Stop
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 转发更新网卡信息命令
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TuntapMessengerIds.UpdateForward)]
        public async Task UpdateForward(IConnection connection)
        {
            TuntapInfo info = MemoryPackSerializer.Deserialize<TuntapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(info.MachineId, out SignCacheInfo cache))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)TuntapMessengerIds.Update,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 广播网卡信息命令，把自己的发给所有人，然后拿回所有人的信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.ConfigForward)]
        public void ConfigForward(IConnection connection)
        {
            TuntapInfo tuntapInfo = MemoryPackSerializer.Deserialize<TuntapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                uint requiestid = connection.ReceiveRequestWrap.RequestId;

                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);
                List<Task<MessageResponeInfo>> tasks = new List<Task<MessageResponeInfo>>();
                foreach (SignCacheInfo item in caches.Where(c => c.MachineId != connection.Id && c.Connected))
                {
                    tasks.Add(messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)TuntapMessengerIds.Config,
                        Payload = connection.ReceiveRequestWrap.Payload,
                        Timeout = 1000,
                    }));
                }

                Task.WhenAll(tasks).ContinueWith(async (result) =>
                {
                    List<TuntapInfo> results = tasks.Where(c => c.Result.Code == MessageResponeCodes.OK)
                    .Select(c => MemoryPackSerializer.Deserialize<TuntapInfo>(c.Result.Data.Span)).ToList();

                    await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        RequestId = requiestid,
                        Connection = connection,
                        Payload = MemoryPackSerializer.Serialize(results)
                    }).ConfigureAwait(false);
                });
            }
        }
    }
}
