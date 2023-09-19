using cmonitor.server.service;
using cmonitor.server.service.messengers.screen;
using cmonitor.server.service.messengers.sign;
using common.libs.extends;

namespace cmonitor.server.api.services
{
    public sealed class ScreenClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly Config config;
        public ScreenClientService(MessengerSender messengerSender, SignCaching signCaching, Config config)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.config = config;
        }
        public bool Update(ClientServiceParamsInfo param)
        {
            string[] names = param.Content.DeJson<string[]>();
            for (int i = 0; i < names.Length; i++)
            {
                bool res = signCaching.Get(names[i], out SignCacheInfo cache)
                    && cache.Connected
                    && cache.GetScreen(config.ScreenDelay)
                    && Interlocked.CompareExchange(ref cache.ScreenFlag, 0, 1) == 1;
                if (res)
                {
                    cache.UpdateScreen();
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ScreenMessengerIds.Update,
                        Timeout = 1000,
                    }).ContinueWith((result) =>
                    {
                        Interlocked.Exchange(ref cache.ScreenFlag, 1);
                    });
                }
            }

            return true;
        }

    }
}
