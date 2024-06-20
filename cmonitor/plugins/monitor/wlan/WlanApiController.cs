using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.wlan.messenger;
using cmonitor.plugins.wlan.report;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;
using common.libs.api;
using cmonitor.server.sapi;

namespace cmonitor.plugins.wlan
{
    public sealed class WlanApiController : IApiServerController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public WlanApiController(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }


        public async Task<List<string>> Get(ApiControllerParamsInfo param)
        {
            List<Task<MessageResponeInfo>> tasks = new List<Task<MessageResponeInfo>>();
            string[] names = param.Content.DeJson<string[]>();
            foreach (string name in names)
            {
                if (signCaching.TryGet(name, out SignCacheInfo cache) && cache.Connected)
                {
                    tasks.Add(messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)WlanMessengerIds.Get,
                        Timeout = 3000
                    }));
                }
            }
            await Task.WhenAll(tasks);

            return tasks.Where(c => c.Result.Code == MessageResponeCodes.OK)
                .SelectMany(c => MemoryPackSerializer.Deserialize<List<string>>(c.Result.Data.Span))
                .Distinct().ToList();
        }

        public async Task<bool> Set(ApiControllerParamsInfo param)
        {
            WlanSetParamInfo info = param.Content.DeJson<WlanSetParamInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Value);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.TryGet(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)WlanMessengerIds.Set,
                        Payload = bytes
                    });
                }
            }

            return true;
        }

    }

    public sealed class WlanSetParamInfo
    {
        public string[] Names { get; set; }
        public WlanSetInfo Value { get; set; }
    }
}
