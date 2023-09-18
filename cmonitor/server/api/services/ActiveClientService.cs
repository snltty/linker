using cmonitor.server.client.reports.active;
using cmonitor.server.service;
using cmonitor.server.service.messengers.active;
using cmonitor.server.service.messengers.sign;
using common.libs;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.server.api.services
{
    public sealed class ActiveClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly RuleConfig ruleConfig;
        public ActiveClientService(MessengerSender messengerSender, SignCaching signCaching, RuleConfig ruleConfig)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.ruleConfig = ruleConfig;
        }
        public async Task<ActiveWindowTimeReportInfo> Get(ClientServiceParamsInfo param)
        {
            if (signCaching.Get(param.Content, out SignCacheInfo cache) && cache.Connected)
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)ActiveMessengerIds.Get
                });
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return MemoryPackSerializer.Deserialize<ActiveWindowTimeReportInfo>(resp.Data.Span);
                }
            }
            return new ActiveWindowTimeReportInfo();
        }

        public async Task<bool> Clear(ClientServiceParamsInfo param)
        {
            if (signCaching.Get(param.Content, out SignCacheInfo cache) && cache.Connected)
            {
                cache.CLearDisallowIds();
                signCaching.Update();
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)ActiveMessengerIds.Clear
                });
                return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
            }
            return false;
        }
        public async Task<bool> Disallow(ClientServiceParamsInfo param)
        {
            DisallowInfo disallowInfo = param.Content.DeJson<DisallowInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(disallowInfo.FileNames);
            foreach (string name in disallowInfo.Names)
            {
                if (signCaching.Get(name, out SignCacheInfo cache) && cache.Connected)
                {
                    if (disallowInfo.Ids != null)
                    {
                        cache.DisallowRunIds = disallowInfo.Ids;
                    }
                    else
                    {
                        cache.CLearDisallowIds();
                    }
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ActiveMessengerIds.Disallow,
                        Payload = bytes
                    });
                }
            }
            signCaching.Update();

            return false;
        }

        public string Add(ClientServiceParamsInfo param)
        {
            return ruleConfig.AddFileName(param.Content.DeJson<AddFileNameInfo>());
        }
        public string Del(ClientServiceParamsInfo param)
        {
            return ruleConfig.DelFileName(param.Content.DeJson<DeletedFileNameInfo>());
        }
    }

    public sealed class DisallowInfo
    {
        public string[] Names { get; set; }
        public string[] FileNames { get; set; }
        public uint[] Ids { get; set; }
    }
}
