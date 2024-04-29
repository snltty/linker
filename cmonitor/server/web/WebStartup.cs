using cmonitor.config;
using cmonitor.startup;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.server.web
{
    public sealed class WebStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<IWebServerServer, WebServerServer>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            IWebServerServer webServer = serviceProvider.GetService<IWebServerServer>();
            webServer.Start(config.Data.Server.WebPort, config.Data.Server.WebRoot);
            Logger.Instance.Info($"server web listen:{config.Data.Server.WebPort}");
        }
    }
}
