using cmonitor.plugins.devices.db;
using cmonitor.server.sapi;
using common.libs.api;
using common.libs.extends;

namespace cmonitor.plugins.devices
{
    public sealed class DevicesApiController : IApiServerController
    {
        private readonly IDevicesDB devicesDB;
        public DevicesApiController(IDevicesDB devicesDB)
        {
            this.devicesDB = devicesDB;
        }

        public string Update(ApiControllerParamsInfo param)
        {
            DevicesUserInfo model = param.Content.DeJson<DevicesUserInfo>();
            devicesDB.Add(model);
            return string.Empty;
        }

    }
}
