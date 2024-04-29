using cmonitor.server.api;
using cmonitor.plugins.light.messenger;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;
using common.libs.api;

namespace cmonitor.plugins.light
{
    public sealed class LightApiController : IApiServerController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public LightApiController(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }


        public async Task<bool> Update(ApiControllerParamsInfo param)
        {
            LightInfo info = param.Content.DeJson<LightInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Value);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)LightMessengerIds.Update,
                        Payload = bytes
                    }); ;
                }
            }

            return true;
        }

    }

    public sealed class LightInfo
    {
        public string[] Names { get; set; }
        public int Value { get; set; }
    }
}
