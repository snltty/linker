using cmonitor.plugins.hijack.messenger;
using cmonitor.plugins.hijack.report;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;
using common.libs.api;
using cmonitor.server.sapi;
using cmonitor.plugins.hijack.db;

namespace cmonitor.plugins.hijack
{
    public sealed class HijackApiController : IApiServerController
    {
        private readonly IHijackDB hijackDB;
        private readonly SignCaching signCaching;
        private readonly MessengerSender messengerSender;
        public HijackApiController(IHijackDB hijackDB, SignCaching signCaching, MessengerSender messengerSender)
        {
            this.hijackDB = hijackDB;
            this.signCaching = signCaching;
            this.messengerSender = messengerSender;
        }

        public string UpdateRule(ApiControllerParamsInfo param)
        {
            HijackRuleUserInfo model = param.Content.DeJson<HijackRuleUserInfo>();
            hijackDB.AddRule(model);
            return string.Empty;
        }


        public string UpdateProcess(ApiControllerParamsInfo param)
        {
            HijackProcessUserInfo model = param.Content.DeJson<HijackProcessUserInfo>();
            hijackDB.AddProcess(model);
            return string.Empty;
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
