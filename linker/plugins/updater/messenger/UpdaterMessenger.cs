using linker.plugins.signin.messenger;
using linker.server;

namespace linker.plugins.updater.messenger
{
    public sealed class UpdaterClientMessenger : IMessenger
    {
        private readonly UpdaterTransfer updaterTransfer;
        public UpdaterClientMessenger(UpdaterTransfer updaterTransfer)
        {
            this.updaterTransfer = updaterTransfer;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            updaterTransfer.Update();
        }
    }


    public sealed class UpdaterServerMessenger : IMessenger
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;

        public UpdaterServerMessenger(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }

        /// <summary>
        /// 广播更新消息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)UpdaterMessengerIds.UpdateForward)]
        public void UpdateForward(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);
                foreach (SignCacheInfo item in caches.Where(c => c.MachineId != connection.Id && c.Connected))
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)UpdaterMessengerIds.Update,
                        Timeout = 1000,
                    });
                }
            }
        }
    }
}
