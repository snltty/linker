using cmonitor.api;
using cmonitor.plugins.display.messenger;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs.extends;

namespace cmonitor.plugins.display
{
    public sealed class DisplayApiController : IApiController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public DisplayApiController(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }

        public async Task<bool> Update(ApiControllerParamsInfo param)
        {
            ScreenDisplayInfo display = param.Content.DeJson<ScreenDisplayInfo>();
            byte[] state = new byte[] { (byte)(display.State ? 1 : 0) };

            for (int i = 0; i < display.Names.Length; i++)
            {
                bool res = signCaching.Get(display.Names[i], out SignCacheInfo cache) && cache.Connected;
                if (res)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)DisplayMessengerIds.Update,
                        Payload = state
                    });
                }
            }

            return true;
        }
    }

    public sealed class ScreenDisplayInfo
    {
        public string[] Names { get; set; }
        public bool State { get; set; }
    }

}
