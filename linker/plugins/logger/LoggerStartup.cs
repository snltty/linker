using linker.config;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace linker.plugins.logger
{
    public sealed class LoggerStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Top;
        public string Name => "logger";

        public bool Required => false;

        public string[] Dependent => new string[] { };

        public StartupLoadType LoadType => StartupLoadType.Normal;


        public void AddClient(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<LoggerClientApiController>();
            if (config.Data.Client.OnlyNode)
            {
                config.Data.Common.LoggerType = libs.LoggerTypes.WARNING;
            }
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {

        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            LoggerClientApiController logger = serviceProvider.GetService<LoggerClientApiController>();

        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
        }
    }
}
