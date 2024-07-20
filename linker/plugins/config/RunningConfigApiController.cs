using linker.libs.api;
using linker.libs.extends;
using linker.client.config;
using linker.plugins.capi;

namespace linker.plugins.config
{
    public sealed class RunningConfigApiController : IApiClientController
    {
        private readonly RunningConfigTransfer runningConfigTransfer;

        public RunningConfigApiController(RunningConfigTransfer runningConfigTransfer)
        {
            this.runningConfigTransfer = runningConfigTransfer;
        }

        public void UpdateVersion(ApiControllerParamsInfo param)
        {
            UpdateVersionInfo info = param.Content.DeJson<UpdateVersionInfo>();
            runningConfigTransfer.UpdateVersion(info.Key, info.Version);
        }

        public sealed class UpdateVersionInfo
        {
            public string Key { get; set; }
            public ulong Version { get; set; }
        }
    }
}
