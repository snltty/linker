using cmonitor.config;
using cmonitor.startup;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.client.web
{
    public sealed class WebStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<IWebClientServer, WebClientServer>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            IWebClientServer webServer = serviceProvider.GetService<IWebClientServer>();
            webServer.Start(config.Data.Client.WebPort, config.Data.Client.WebRoot);
            Logger.Instance.Info($"client web listen:{config.Data.Client.WebPort}");
        }
    }
}
