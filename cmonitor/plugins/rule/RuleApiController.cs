using cmonitor.api;
using cmonitor.server.ruleConfig;

namespace cmonitor.plugins.rule
{
    public sealed class RuleApiController : IApiController
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
