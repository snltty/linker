using cmonitor.config;
using cmonitor.startup;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.sapi
{
    public sealed class SApiStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "sapi";
        public bool Required => false;
        public string[] Dependent => new string[] { };
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<IWebServerServer, WebServerServer>();
            serviceCollection.AddSingleton<IApiServerServer, ApiServerServer>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            if (config.Data.Server.SApi.WebPort > 0)
            {
                IWebServerServer webServer = serviceProvider.GetService<IWebServerServer>();
                webServer.Start(config.Data.Server.SApi.WebPort, config.Data.Server.SApi.WebRoot);
                Logger.Instance.Info($"server web listen:{config.Data.Server.SApi.WebPort}");
            }
            if (config.Data.Server.SApi.ApiPort > 0)
            {
                Logger.Instance.Info($"start server api ");
                IApiServerServer clientServer = serviceProvider.GetService<IApiServerServer>();
                clientServer.LoadPlugins(assemblies);
                clientServer.Websocket(config.Data.Server.SApi.ApiPort, config.Data.Server.SApi.ApiPassword);
                Logger.Instance.Info($"server api listen:{config.Data.Server.SApi.ApiPort}");
                Logger.Instance.Info($"server api password:{config.Data.Server.SApi.ApiPassword}");
            }
        }
    }
}
