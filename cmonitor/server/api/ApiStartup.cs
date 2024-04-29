using cmonitor.config;
using cmonitor.startup;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.server.api
{
    public sealed class ApiStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<IApiServerServer, ApiServerServer>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            Logger.Instance.Info($"start client api server");
            IApiServerServer clientServer = serviceProvider.GetService<IApiServerServer>();
            clientServer.LoadPlugins(assemblies);
            clientServer.Websocket(config.Data.Server.ApiPort, config.Data.Server.ApiPassword);
            Logger.Instance.Info($"client api listen:{config.Data.Server.ApiPort}");
            Logger.Instance.Info($"client api password:{config.Data.Server.ApiPassword}");
        }
    }
}
