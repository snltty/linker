using linker.config;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.logger
{
    public sealed class LoggerStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Top;
        public string Name => "logger";

        public bool Required => false;

        public string[] Dependent => new string[] { };

        public StartupLoadType LoadType => StartupLoadType.Normal;


        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<LoggerClientApiController>();
            if (config.Data.Client.HasAccess(ClientApiAccess.LoggerLevel) == false)
            {
                config.Data.Common.LoggerType = libs.LoggerTypes.WARNING;
            }
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {

        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            LoggerClientApiController logger = serviceProvider.GetService<LoggerClientApiController>();

        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
        }
    }
}
