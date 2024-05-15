using cmonitor.plugins.sapi;
using cmonitor.server.ruleConfig;
using common.libs.api;
using common.libs.extends;

namespace cmonitor.plugins.devices
{
    public sealed class DevicesApiController : IApiServerController
    {
        private readonly IRuleConfig ruleConfig;
        public DevicesApiController(IRuleConfig ruleConfig)
        {
            this.ruleConfig = ruleConfig;
        }

        public string Update(ApiControllerParamsInfo param)
        {
            UpdateDevicesInfo model = param.Content.DeJson<UpdateDevicesInfo>();
            ruleConfig.Set(model.UserName, "Devices", model.Data);
            return string.Empty;
        }
        public sealed class UpdateDevicesInfo
        {
            public string UserName { get; set; }
            public List<string> Data { get; set; } = new List<string>();
        }

    }
}
