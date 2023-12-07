using cmonitor.client.reports.hijack;
using cmonitor.service;
using cmonitor.service.messengers.hijack;
using cmonitor.service.messengers.sign;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.api.services
{
    public sealed class HijackClientService : IClientService
    {
        private readonly RuleConfig ruleConfig;
        private readonly SignCaching signCaching;
        private readonly MessengerSender messengerSender;
        public HijackClientService(RuleConfig ruleConfig, SignCaching signCaching, MessengerSender messengerSender)
        {
            this.ruleConfig = ruleConfig;
            this.signCaching = signCaching;
            this.messengerSender = messengerSender;
        }
        public Dictionary<string, UserNameInfo> Info(ClientServiceParamsInfo param)
        {
            return ruleConfig.UserNames;
        }
        public string AddName(ClientServiceParamsInfo param)
        {
            return ruleConfig.AddName(param.Content);
        }

        public string AddProcessGroup(ClientServiceParamsInfo param)
        {
            return ruleConfig.AddProcessGroup(param.Content.DeJson<UpdateGroupInfo>());
        }
        public string DeleteProcessGroup(ClientServiceParamsInfo param)
        {
            return ruleConfig.DeleteProcessGroup(param.Content.DeJson<DeleteGroupInfo>());
        }
        public string AddProcess(ClientServiceParamsInfo param)
        {
            return ruleConfig.AddProcess(param.Content.DeJson<UpdateItemInfo>());
        }
        public string DeleteProcess(ClientServiceParamsInfo param)
        {
            return ruleConfig.DeleteProcess(param.Content.DeJson<DeleteItemInfo>());
        }


        public string AddRule(ClientServiceParamsInfo param)
        {
            return ruleConfig.AddRule(param.Content.DeJson<UpdateRuleInfo>());
        }
        public string DeleteRule(ClientServiceParamsInfo param)
        {
            return ruleConfig.DeleteRule(param.Content.DeJson<DeleteRuleInfo>());
        }

        public string UpdateDevices(ClientServiceParamsInfo param)
        {
            return ruleConfig.UpdateDevices(param.Content.DeJson<UpdateDevicesInfo>());
        }

        public async Task<List<string>> SetRules(ClientServiceParamsInfo param)
        {
            List<string> errorDevices = new List<string>();
            SetRuleParamInfo setRuleParamInfo = param.Content.DeJson<SetRuleParamInfo>();
            if (setRuleParamInfo.Devices.Length > 0)
            {
                byte[] bytes = MemoryPackSerializer.Serialize(setRuleParamInfo.Rules);
                for (int i = 0; i < setRuleParamInfo.Devices.Length; i++)
                {
                    if (signCaching.Get(setRuleParamInfo.Devices[i], out SignCacheInfo cache))
                    {
                        if (setRuleParamInfo.Ids != null)
                        {
                            cache.RuleIds = setRuleParamInfo.Ids;
                        }
                        else
                        {
                            cache.CLearRuleIds();
                        }

                        bool res = await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = cache.Connection,
                            MessengerId = (ushort)HijackMessengerIds.Update,
                            Payload = bytes
                        });
                        if (res == false)
                        {
                            errorDevices.Add(setRuleParamInfo.Devices[i]);
                        }
                    }
                }
                signCaching.Update();
            }
            return errorDevices;
        }

        public sealed class SetRuleParamInfo
        {
            public string[] Devices { get; set; }
            public SetRuleInfo Rules { get; set; }
            public uint[] Ids { get; set; }
        }
    }
}
