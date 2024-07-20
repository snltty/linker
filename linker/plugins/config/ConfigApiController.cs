using linker.config;
using linker.libs.api;
using linker.libs.extends;
using linker.client.config;
using linker.plugins.capi;

namespace linker.plugins.config
{
    public sealed class ConfigClientApiController : IApiClientController
    {
        private readonly RunningConfig runningConfig;
        private readonly FileConfig config;

        public ConfigClientApiController(RunningConfig runningConfig, FileConfig config)
        {
            this.runningConfig = runningConfig;
            this.config = config;

        }

        public object Get(ApiControllerParamsInfo param)
        {
            return new { Common = config.Data.Common, Client = config.Data.Client, Server = config.Data.Server, Running = runningConfig.Data };
        }

        public bool Install(ApiControllerParamsInfo param)
        {
            ConfigInstallInfo info = param.Content.DeJson<ConfigInstallInfo>();

            if (info.Common.Modes.Contains("client"))
            {
                config.Data.Client.Name = info.Client.Name;
                config.Data.Client.GroupId = info.Client.GroupId;
                config.Data.Client.CApi.WebPort = info.Client.Web;
                config.Data.Client.CApi.ApiPort = info.Client.Api;
                config.Data.Client.CApi.ApiPassword = info.Client.Password;
            }
            if (info.Common.Modes.Contains("server"))
            {
                config.Data.Server.ServicePort = info.Server.ServicePort;
                config.Data.Server.Relay.SecretKey = info.Server.Relay.SecretKey;

                config.Data.Server.SForward.SecretKey = info.Server.SForward.SecretKey;
                config.Data.Server.SForward.WebPort = info.Server.SForward.WebPort;
                config.Data.Server.SForward.TunnelPortRange = info.Server.SForward.TunnelPortRange;

                config.Data.Server.Updater.SecretKey = info.Server.Updater.SecretKey;
            }

            config.Data.Common.Modes = info.Common.Modes;
            config.Data.Common.Install = true;
            config.Data.Update();
            return true;
        }
    }

    public sealed class ConfigInstallInfo
    {
        public ConfigInstallClientInfo Client { get; set; } = new ConfigInstallClientInfo();
        public ConfigInstallServerInfo Server { get; set; } = new ConfigInstallServerInfo();
        public ConfigInstallCommonInfo Common { get; set; } = new ConfigInstallCommonInfo();
    }
    public sealed class ConfigInstallClientInfo
    {
        public string Name { get; set; }
        public string GroupId { get; set; }
        public int Api { get; set; }
        public int Web { get; set; }
        public string Password { get; set; }
    }
    public sealed class ConfigInstallServerInfo
    {
        public int ServicePort { get; set; }
        public ConfigInstallServerRelayInfo Relay { get; set; }
        public ConfigInstallServerSForwardInfo SForward { get; set; }
        public ConfigInstallServerUpdaterInfo Updater { get; set; }
    }
    public sealed class ConfigInstallServerUpdaterInfo
    {
        public string SecretKey { get; set; }
    }
    public sealed class ConfigInstallServerRelayInfo
    {
        public string SecretKey { get; set; }
    }
    public sealed class ConfigInstallServerSForwardInfo
    {
        public string SecretKey { get; set; }
        public int WebPort { get; set; }
        public int[] TunnelPortRange { get; set; }
    }

    public sealed class ConfigInstallCommonInfo
    {
        public string[] Modes { get; set; }
    }
}
