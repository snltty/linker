using linker.config;
using linker.libs.api;
using linker.libs.extends;
using linker.client.config;
using linker.plugins.capi;

namespace linker.plugins.net
{
    public sealed class NetClientApiController : IApiClientController
    {
        private readonly RunningConfig runningConfig;
        private readonly FileConfig config;

        public NetClientApiController(RunningConfig runningConfig, FileConfig config)
        {
            this.runningConfig = runningConfig;
            this.config = config;

        }

        public bool Save(ApiControllerParamsInfo param)
        {
            NetSaveInfo info = param.Content.DeJson<NetSaveInfo>();

            config.Data.Client.Server = info.Host;
            config.Data.Client.GroupId = info.GroupId;

            config.Data.Common.Install = true;
            config.Data.Common.Modes = ["client"];
            config.Data.Update();

            if (runningConfig.Data.Relay.Servers.Length == 0)
            {
                runningConfig.Data.Relay.Servers = new RelayServerInfo[] { new RelayServerInfo { Name = "default", RelayType = RelayType.Linker, SSL = true, SecretKey = info.RelaySecretKey, Host = info.Host } };
            }
            runningConfig.Data.Relay.Servers[0].Host = info.RelaySecretKey;
            runningConfig.Data.Relay.Servers[0].SecretKey = info.RelaySecretKey;
            runningConfig.Data.Update();

            return true;
        }
    }

    public sealed class NetSaveInfo
    {
        public string Host { get; set; }
        public string GroupId { get; set; }
        public string RelaySecretKey { get; set; }
    }
}
