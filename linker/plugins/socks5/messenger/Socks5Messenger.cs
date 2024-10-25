using linker.config;
using linker.plugins.messenger;
using linker.plugins.signin.messenger;
using linker.plugins.socks5.config;
using MemoryPack;

namespace linker.plugins.socks5.messenger
{
    public sealed class Socks5ClientMessenger : IMessenger
    {
        private readonly Socks5ConfigTransfer socks5ConfigTransfer;
        private readonly TunnelProxy socks5Proxy;
        public Socks5ClientMessenger(Socks5ConfigTransfer socks5ConfigTransfer, TunnelProxy socks5Proxy)
        {
            this.socks5ConfigTransfer = socks5ConfigTransfer;
            this.socks5Proxy = socks5Proxy;
        }

        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)Socks5MessengerIds.Run)]
        public void Run(IConnection connection)
        {
            socks5ConfigTransfer.Retstart();
        }

        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)Socks5MessengerIds.Stop)]
        public void Stop(IConnection connection)
        {
            socks5ConfigTransfer.Stop();
        }

        /// <summary>
        /// 更新信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)Socks5MessengerIds.Update)]
        public void Update(IConnection connection)
        {
            Socks5Info info = MemoryPackSerializer.Deserialize<Socks5Info>(connection.ReceiveRequestWrap.Payload.Span);
            socks5ConfigTransfer.UpdateConfig(info);
        }

    }


    public sealed class Socks5ServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly FileConfig config;

        public Socks5ServerMessenger(IMessengerSender messengerSender, SignCaching signCaching, FileConfig config)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.config = config;
        }

        /// <summary>
        /// 转发运行命令
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)Socks5MessengerIds.RunForward)]
        public async Task RunForward(IConnection connection)
        {
            string name = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(name, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)Socks5MessengerIds.Run
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 转发停止命令
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)Socks5MessengerIds.StopForward)]
        public async Task StopForward(IConnection connection)
        {
            string name = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(name, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)Socks5MessengerIds.Stop
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 转发更新信息命令
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)Socks5MessengerIds.UpdateForward)]
        public async Task UpdateForward(IConnection connection)
        {
            Socks5Info info = MemoryPackSerializer.Deserialize<Socks5Info>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(info.MachineId, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)Socks5MessengerIds.Update,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ConfigureAwait(false);
            }
        }
    }
}
