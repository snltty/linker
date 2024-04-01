using cmonitor.api;
using cmonitor.client.ruleConfig;
using cmonitor.plugins.signIn.messenger;
using cmonitor.server;
using common.libs.extends;

namespace cmonitor.plugins.modes
{
    public sealed class ModesApiController : IApiController
    {
        private readonly RuleConfig ruleConfig;
        private readonly SignCaching signCaching;
        private readonly MessengerSender messengerSender;
        public ModesApiController(RuleConfig ruleConfig, SignCaching signCaching, MessengerSender messengerSender)
        {
            this.ruleConfig = ruleConfig;
            this.signCaching = signCaching;
            this.messengerSender = messengerSender;
        }

        public string Update(ApiControllerParamsInfo param)
        {
            return ruleConfig.UpdateModes(param.Content.DeJson<UpdateModesInfo>());
        }
    }
}
