using cmonitor.plugins.active.db;
using cmonitor.plugins.devices.db;
using cmonitor.plugins.hijack.db;
using cmonitor.plugins.modes.db;
using cmonitor.plugins.snatch.db;
using cmonitor.server.sapi;
using common.libs.api;

namespace cmonitor.plugins.rule
{
    public sealed class RuleApiController : IApiServerController
    {
        private readonly IActiveWindowDB activeWindowDB;
        private readonly IDevicesDB devicesDB;
        private readonly IHijackDB hijackDB;
        private readonly IModesDB modesDB;
        private readonly ISnatchDB snatchDB;


        public RuleApiController(IActiveWindowDB activeWindowDB, IDevicesDB devicesDB, IHijackDB hijackDB, IModesDB modesDB, ISnatchDB snatchDB)
        {
            this.activeWindowDB = activeWindowDB;
            this.devicesDB = devicesDB;
            this.hijackDB = hijackDB;
            this.modesDB = modesDB;
            this.snatchDB = snatchDB;
        }

        public Dictionary<string, Dictionary<string, object>> Info(ApiControllerParamsInfo param)
        {
            Dictionary<string, Dictionary<string, object>> result = new Dictionary<string, Dictionary<string, object>>();

            foreach (var item in activeWindowDB.Get())
            {
                Add(result, item.UserName, "Windows", item.Data);
            }
            foreach (var item in devicesDB.Get())
            {
                Add(result, item.UserName, "Devices", item.Data);
            }
            foreach (var item in hijackDB.GetRule())
            {
                Add(result, item.UserName, "Rules", item.Data);
            }
            foreach (var item in hijackDB.GetProcess())
            {
                Add(result, item.UserName, "Processs", item.Data);
            }
            foreach (var item in modesDB.Get())
            {
                Add(result, item.UserName, "Modes", item.Data);
            }
            foreach (var item in snatchDB.Get())
            {
                Add(result, item.UserName, "Snatchs", item.Data);
            }
            return result;

        }

        private void Add(Dictionary<string, Dictionary<string, object>> result, string username, string key, object value)
        {
            if (result.TryGetValue(username, out Dictionary<string, object> val) == false)
            {
                val = new Dictionary<string, object>();
                result.Add(username, val);
            }
            val[key] = value;
        }
    }
}
