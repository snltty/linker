using linker.config;
using linker.plugins.messenger;
using linker.plugins.signin.messenger;
using linker.plugins.tuntap.config;
using linker.plugins.tuntap.lease;
using MemoryPack;

namespace linker.plugins.tuntap.messenger
{
    public sealed class TuntapClientMessenger : IMessenger
    {
        private readonly TuntapTransfer tuntapTransfer;
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly TuntapProxy tuntapProxy;
        private readonly LeaseClientTreansfer leaseClientTreansfer;
        private readonly TuntapPingTransfer pingTransfer;

        public TuntapClientMessenger(TuntapTransfer tuntapTransfer, TuntapConfigTransfer tuntapConfigTransfer, TuntapProxy tuntapProxy, LeaseClientTreansfer leaseClientTreansfer, TuntapPingTransfer pingTransfer)
        {
            this.tuntapTransfer = tuntapTransfer;
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.tuntapProxy = tuntapProxy;
            this.leaseClientTreansfer = leaseClientTreansfer;
            this.pingTransfer = pingTransfer;
        }

        /// <summary>
        /// 运行网卡
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.Run)]
        public void Run(IConnection connection)
        {
            _ = tuntapConfigTransfer.RetstartDevice();
        }

        /// <summary>
        /// 停止网卡
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.Stop)]
        public void Stop(IConnection connection)
        {
            tuntapConfigTransfer.StopDevice();
        }

        /// <summary>
        /// 更新网卡信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            TuntapInfo info = MemoryPackSerializer.Deserialize<TuntapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            tuntapConfigTransfer.UpdateConfig(info);
        }

        /// <summary>
        /// 重新租赁
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.LeaseChange)]
        public void LeaseChange(IConnection connection)
        {
            tuntapConfigTransfer.RefreshIP();
        }


        [MessengerId((ushort)TuntapMessengerIds.SubscribeForwardTest)]
        public void SubscribeForwardTest(IConnection connection)
        {
            pingTransfer.SubscribeForwardTest();
        }
    }


    public sealed class TuntapServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly FileConfig config;
        private readonly LeaseServerTreansfer leaseTreansfer;

        public TuntapServerMessenger(IMessengerSender messengerSender, SignCaching signCaching, FileConfig config, LeaseServerTreansfer leaseTreansfer)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.config = config;
            this.leaseTreansfer = leaseTreansfer;
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
            if (signCaching.TryGet(name, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
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
            if (signCaching.TryGet(name, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
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
            if (signCaching.TryGet(info.MachineId, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
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
        /// 添加网络配置
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TuntapMessengerIds.LeaseAddNetwork)]
        public void LeaseAddNetwork(IConnection connection)
        {
            LeaseInfo info = MemoryPackSerializer.Deserialize<LeaseInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                leaseTreansfer.AddNetwork(cache.GroupId, info);
            }
        }
        /// <summary>
        /// 添加网络配置
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TuntapMessengerIds.LeaseGetNetwork)]
        public void LeaseGetNetwork(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                connection.Write(MemoryPackSerializer.Serialize(leaseTreansfer.GetNetwork(cache.GroupId)));
            }
        }
        /// <summary>
        /// 租赁IP
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TuntapMessengerIds.LeaseIP)]
        public void LeaseIP(IConnection connection)
        {
            LeaseInfo info = MemoryPackSerializer.Deserialize<LeaseInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                LeaseInfo result = leaseTreansfer.LeaseIP(cache.MachineId, cache.GroupId, info);
                connection.Write(MemoryPackSerializer.Serialize(result));
            }
        }
        /// <summary>
        /// 网络配置发生变化，需要重新租赁
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.LeaseChangeForward)]
        public void LeaseChangeForward(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                uint requiestid = connection.ReceiveRequestWrap.RequestId;

                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);
                IEnumerable<Task<bool>> tasks = caches.Where(c => c.MachineId != connection.Id && c.Connected).Select(c => messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = c.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.LeaseChange,
                    Payload = connection.ReceiveRequestWrap.Payload,
                    Timeout = 1000,
                })).ToList();
                Task.WhenAll(tasks);
            }
        }


        /// <summary>
        /// 续期
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.LeaseExp)]
        public void LeaseExp(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                leaseTreansfer.LeaseExp(cache.MachineId, cache.GroupId);
            }
        }



        [MessengerId((ushort)TuntapMessengerIds.SubscribeForwardTestForward)]
        public async Task SubscribeForwardTestForward(IConnection connection)
        {
            string machineid = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(machineid, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.SubscribeForwardTest
                }).ConfigureAwait(false);
            }
        }
    }
}
