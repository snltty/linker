using cmonitor.plugins.modes.db;
using cmonitor.server.sapi;
using common.libs.api;
using common.libs.extends;

namespace cmonitor.plugins.modes
{
    public sealed class ModesApiController : IApiServerController
    {

        private readonly IModesDB modesDB;
        public ModesApiController(IModesDB modesDB)
        {
            this.modesDB = modesDB;
        }

        public string Update(ApiControllerParamsInfo param)
        {
            ModesUserInfo info = param.Content.DeJson<ModesUserInfo>();
            modesDB.Add(info);
            return string.Empty;
        }


    }
}
