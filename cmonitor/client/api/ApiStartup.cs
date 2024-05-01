using cmonitor.config;
using cmonitor.startup;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.client.api
{
    public sealed class ApiStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<IApiClientServer, ApiClientServer>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            Logger.Instance.Info($"start client api server");
            IApiClientServer clientServer = serviceProvider.GetService<IApiClientServer>();
            clientServer.LoadPlugins(assemblies);
            clientServer.Websocket(config.Data.Client.ApiPort, config.Data.Client.ApiPassword);
            Logger.Instance.Info($"client api listen:{config.Data.Client.ApiPort}");
            Logger.Instance.Info($"client api password:{config.Data.Client.ApiPassword}");
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            
        }
    }
}
