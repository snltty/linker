using cmonitor.server.api;
using cmonitor.server.ruleConfig;
using common.libs.api;

namespace cmonitor.plugins.rule
{
    public sealed class RuleApiController : IApiServerController
    {
        private readonly IRuleConfig ruleConfig;
        public RuleApiController(IRuleConfig ruleConfig)
        {
            this.ruleConfig = ruleConfig;
        }

        public RuleConfigInfo Info(ApiControllerParamsInfo param)
        {
            return ruleConfig.Data;
        }
        public string AddName(ApiControllerParamsInfo param)
        {
            ruleConfig.AddUser(param.Content);
            return string.Empty;
        }
    }
}
