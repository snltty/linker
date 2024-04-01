using cmonitor.api;
using cmonitor.client.ruleConfig;
using cmonitor.plugins.hijack.messenger;
using cmonitor.plugins.hijack.report;
using cmonitor.plugins.signIn.messenger;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.plugins.hijack
{
    public sealed class HijackApiController : IApiController
    {
        private readonly RuleConfig ruleConfig;
        private readonly SignCaching signCaching;
        private readonly MessengerSender messengerSender;
        public HijackApiController(RuleConfig ruleConfig, SignCaching signCaching, MessengerSender messengerSender)
        {
            this.ruleConfig = ruleConfig;
            this.signCaching = signCaching;
            this.messengerSender = messengerSender;
        }
        public Dictionary<string, UserNameInfo> Info(ApiControllerParamsInfo param)
        {
            return ruleConfig.UserNames;
        }
        public string AddName(ApiControllerParamsInfo param)
        {
            return ruleConfig.AddName(param.Content);
        }

        public string AddProcessGroup(ApiControllerParamsInfo param)
        {
            return ruleConfig.AddProcessGroup(param.Content.DeJson<UpdateGroupInfo>());
        }
        public string DeleteProcessGroup(ApiControllerParamsInfo param)
        {
            return ruleConfig.DeleteProcessGroup(param.Content.DeJson<DeleteGroupInfo>());
        }
        public string AddProcess(ApiControllerParamsInfo param)
        {
            return ruleConfig.AddProcess(param.Content.DeJson<UpdateItemInfo>());
        }
        public string DeleteProcess(ApiControllerParamsInfo param)
        {
            return ruleConfig.DeleteProcess(param.Content.DeJson<DeleteItemInfo>());
        }


        public string AddRule(ApiControllerParamsInfo param)
        {
            return ruleConfig.AddRule(param.Content.DeJson<UpdateRuleInfo>());
        }
        public string DeleteRule(ApiControllerParamsInfo param)
        {
            return ruleConfig.DeleteRule(param.Content.DeJson<DeleteRuleInfo>());
        }

        public string UpdateDevices(ApiControllerParamsInfo param)
        {
            return ruleConfig.UpdateDevices(param.Content.DeJson<UpdateDevicesInfo>());
        }

        public async Task<List<string>> SetRules(ApiControllerParamsInfo param)
        {
            List<string> errorDevices = new List<string>();
            SetRuleParamInfo setRuleParamInfo = param.Content.DeJson<SetRuleParamInfo>();
            if (setRuleParamInfo.Devices.Length > 0)
            {
                byte[] bytes = MemoryPackSerializer.Serialize(new HijackSetRuleInfo
                {
                    Rules = setRuleParamInfo.Rules,
                    Ids = setRuleParamInfo.Ids,

                });
                for (int i = 0; i < setRuleParamInfo.Devices.Length; i++)
                {
                    if (signCaching.Get(setRuleParamInfo.Devices[i], out SignCacheInfo cache))
                    {
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
            }
            return errorDevices;
        }

        public sealed class SetRuleParamInfo
        {
            public string[] Devices { get; set; }
            public HijackRuleUpdateInfo Rules { get; set; }
            public uint[] Ids { get; set; }
        }
    }
}
