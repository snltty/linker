using cmonitor.server.client.reports.snatch;
using cmonitor.server.service;
using cmonitor.server.service.messengers.sign;
using cmonitor.server.service.messengers.snatch;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.server.api.services
{
    public sealed class SnatchClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly RuleConfig ruleConfig;
        private readonly SignCaching signCaching;
        public SnatchClientService(RuleConfig ruleConfig, MessengerSender messengerSender, SignCaching signCaching)
        {
            this.ruleConfig = ruleConfig;
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }

        public string AddGroup(ClientServiceParamsInfo param)
        {
            return ruleConfig.AddSnatchGroup(param.Content.DeJson<UpdateSnatchGroupInfo>());
        }
        public string DeleteGroup(ClientServiceParamsInfo param)
        {
            return ruleConfig.DeleteSnatchGroup(param.Content.DeJson<DeleteSnatchGroupInfo>());
        }
        public string Add(ClientServiceParamsInfo param)
        {
            return ruleConfig.AddSnatch(param.Content.DeJson<AddSnatchItemInfo>());
        }
        public string Del(ClientServiceParamsInfo param)
        {
            return ruleConfig.DelSnatch(param.Content.DeJson<DeletedSnatchItemInfo>());
        }

        public async Task<bool> Update(ClientServiceParamsInfo param)
        {
            SnatchUpdateInfo info = param.Content.DeJson<SnatchUpdateInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Item);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache))
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)SnatchMessengerIds.Update,
                        Payload = bytes
                    });
                }
            }

            return true;
        }
    }

    public sealed class SnatchUpdateInfo
    {
        public string[] Names { get; set; }
        public SnatchQuestionInfo Item { get; set; }
    }
}
