using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.socks5
{
    public sealed class Socks5ClientMessenger : IMessenger
    {
        private readonly Socks5Transfer socks5Transfer;
        private readonly Socks5Proxy socks5Proxy;
        private readonly ISerializer serializer;
        public Socks5ClientMessenger(Socks5Transfer socks5Transfer, Socks5Proxy socks5Proxy, ISerializer serializer)
        {
            this.socks5Transfer = socks5Transfer;
            this.socks5Proxy = socks5Proxy;
            this.serializer = serializer;
        }

        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)Socks5MessengerIds.Run)]
        public void Run(IConnection connection)
        {
            socks5Transfer.Retstart();
        }

        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)Socks5MessengerIds.Stop)]
        public void Stop(IConnection connection)
        {
            socks5Transfer.Stop();
        }

        /// <summary>
        /// 更新信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)Socks5MessengerIds.Update)]
        public void Update(IConnection connection)
        {
            Socks5Info info = serializer.Deserialize<Socks5Info>(connection.ReceiveRequestWrap.Payload.Span);
            socks5Transfer.UpdateConfig(info);
        }

    }


    public sealed class Socks5ServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;
        public Socks5ServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }

        /// <summary>
        /// 转发运行命令
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)Socks5MessengerIds.RunForward)]
        public async Task RunForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
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
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id,machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
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
            Socks5Info info = serializer.Deserialize<Socks5Info>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)Socks5MessengerIds.Update,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ConfigureAwait(false);
            }
        }
    }
}
