using cmonitor.client.reports.active;
using cmonitor.service;
using cmonitor.service.messengers.active;
using cmonitor.service.messengers.sign;
using common.libs;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.api.services
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
        public async Task<Dictionary<uint, string>> Windows(ClientServiceParamsInfo param)
        {
            if (signCaching.Get(param.Content, out SignCacheInfo cache) && cache.Connected)
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)ActiveMessengerIds.Windows
                });
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return MemoryPackSerializer.Deserialize<Dictionary<uint, string>>(resp.Data.Span);
                }
            }
            return new Dictionary<uint, string>();
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
            foreach (string name in disallowInfo.UserNames)
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


        public string AddGroup(ClientServiceParamsInfo param)
        {
            return ruleConfig.AddWindowGroup(param.Content.DeJson<UpdateWindowGroupInfo>());
        }
        public string DeleteGroup(ClientServiceParamsInfo param)
        {
            return ruleConfig.DeleteWindowGroup(param.Content.DeJson<DeleteWindowGroupInfo>());
        }
        public string Add(ClientServiceParamsInfo param)
        {
            return ruleConfig.AddWindow(param.Content.DeJson<AddWindowItemInfo>());
        }
        public string Del(ClientServiceParamsInfo param)
        {
            return ruleConfig.DelWindow(param.Content.DeJson<DeletedWindowItemInfo>());
        }
    }

    public sealed class DisallowInfo
    {
        public string[] UserNames { get; set; }
        public string[] FileNames { get; set; }
        public uint[] Ids { get; set; }
    }
}
