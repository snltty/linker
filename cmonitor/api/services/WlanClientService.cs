using cmonitor.client.reports.wlan;
using cmonitor.service;
using cmonitor.service.messengers.sign;
using cmonitor.service.messengers.wlan;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.api.services
{
    public sealed class WlanClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public WlanClientService(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }


        public async Task<List<string>> Get(ClientServiceParamsInfo param)
        {
            List<Task<MessageResponeInfo>> tasks = new List<Task<MessageResponeInfo>>();
            string[] names = param.Content.DeJson<string[]>();
            foreach (string name in names)
            {
                if (signCaching.Get(name, out SignCacheInfo cache) && cache.Connected)
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

        public async Task<bool> Set(ClientServiceParamsInfo param)
        {
            WlanSetParamInfo info = param.Content.DeJson<WlanSetParamInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Value);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
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
