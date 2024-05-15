using cmonitor.plugins.hijack.messenger;
using cmonitor.plugins.hijack.report;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using cmonitor.server.ruleConfig;
using common.libs.extends;
using MemoryPack;
using common.libs.api;
using cmonitor.plugins.sapi;

namespace cmonitor.plugins.hijack
{
    public sealed class HijackApiController : IApiServerController
    {
        private readonly IRuleConfig ruleConfig;
        private readonly SignCaching signCaching;
        private readonly MessengerSender messengerSender;
        public HijackApiController(IRuleConfig ruleConfig, SignCaching signCaching, MessengerSender messengerSender)
        {
            this.ruleConfig = ruleConfig;
            this.signCaching = signCaching;
            this.messengerSender = messengerSender;
        }

        public string UpdateRule(ApiControllerParamsInfo param)
        {
            UpdateRuleInfo model = param.Content.DeJson<UpdateRuleInfo>();
            ruleConfig.Set(model.UserName,"Rules", model.Data);
            return string.Empty;
        }
        public sealed class RulesInfo
        {
            public string Name { get; set; }
            public List<string> PrivateProcesss { get; set; } = new List<string>();
            public List<string> PublicProcesss { get; set; } = new List<string>();
        }
        public sealed class UpdateRuleInfo
        {
            public string UserName { get; set; }
            public List<RulesInfo> Data { get; set; }
        }


        public string UpdateProcess(ApiControllerParamsInfo param)
        {
            UpdateHijackInfo model = param.Content.DeJson<UpdateHijackInfo>();
            ruleConfig.Set(model.UserName, "Processs", model.Data);
            return string.Empty;
        }
        public sealed class HijackGroupInfo
        {
            public string Name { get; set; }
            public List<HijackItemInfo> List { get; set; } = new List<HijackItemInfo>();
        }
        public sealed class HijackItemInfo
        {
            public string Name { get; set; }
            public HijackDataType DataType { get; set; }
            public HijackAllowType AllowType { get; set; }
        }
        public enum HijackDataType
        {
            Process = 0,
            Domain = 1,
            IP = 2,
        }
        public enum HijackAllowType
        {
            Allow = 0,
            Denied = 1
        }
        public sealed class UpdateHijackInfo
        {
            public string UserName { get; set; }
            public List<HijackGroupInfo> Data { get; set; }
        }



        public async Task<List<string>> UseHijackRules(ApiControllerParamsInfo param)
        {
            List<string> errorDevices = new List<string>();
            SetRuleParamInfo setRuleParamInfo = param.Content.DeJson<SetRuleParamInfo>();
            if (setRuleParamInfo.Devices.Length > 0)
            {
                byte[] bytes = MemoryPackSerializer.Serialize(new HijackSetRuleInfo
                {
                    Rules = setRuleParamInfo.Data,
                    Ids1 = setRuleParamInfo.Ids1,
                    Ids2 = setRuleParamInfo.Ids2,
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
            public HijackRuleUpdateInfo Data { get; set; }
            public string[] Ids1 { get; set; }
            public string[] Ids2 { get; set; }
        }
    }
}
