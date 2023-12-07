using cmonitor.client.reports.snatch;
using cmonitor.service;
using cmonitor.service.messengers.sign;
using cmonitor.service.messengers.snatch;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.api.services
{
    public sealed class SnatchClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly RuleConfig ruleConfig;
        private readonly SignCaching signCaching;
        private readonly ISnatachCaching snatachCaching;

        public SnatchClientService(RuleConfig ruleConfig, MessengerSender messengerSender, SignCaching signCaching, ISnatachCaching snatachCaching)
        {
            this.ruleConfig = ruleConfig;
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.snatachCaching = snatachCaching;
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


        public SnatchQuestionCacheInfo GetQuestion(ClientServiceParamsInfo param)
        {
            snatachCaching.Get(param.Content, out SnatchQuestionCacheInfo info);
            return info;
        }
        public async Task<bool> AddQuestion(ClientServiceParamsInfo param)
        {
            SnatchQuestionCacheInfo info = param.Content.DeJson<SnatchQuestionCacheInfo>();
            if (snatachCaching.Add(info) && snatachCaching.Get(info.Name, out info))
            {
                byte[] bytes = info.Question.ToBytes();
                for (int i = 0; i < info.Names.Length; i++)
                {
                    if (signCaching.Get(info.Names[i], out SignCacheInfo cache))
                    {
                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = cache.Connection,
                            MessengerId = (ushort)SnatchMessengerIds.AddQuestion,
                            Payload = bytes
                        });
                    }
                }
            }
            return true;
        }
        public async Task<bool> RemoveQuestion(ClientServiceParamsInfo param)
        {
            if (snatachCaching.Remove(param.Content, out SnatchQuestionCacheInfo info))
            {
                for (int i = 0; i < info.Names.Length; i++)
                {
                    if (signCaching.Get(info.Names[i], out SignCacheInfo cache))
                    {
                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = cache.Connection,
                            MessengerId = (ushort)SnatchMessengerIds.RemoveQuestion
                        });
                    }
                }
            }
            return true;
        }
    }

}
