using link.client.capi;
using link.config;
using link.startup;
using link.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace link.plugins.capi
{
    public sealed class CApiStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "capi";
        public bool Required => false;
        public string[] Dependent => new string[] {};
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<IApiClientServer, ApiClientServer>();
            serviceCollection.AddSingleton<IWebClientServer, WebClientServer>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            if (config.Data.Client.CApi.ApiPort > 0)
            {
                Logger.Instance.Info($"start client api server");
                IApiClientServer clientServer = serviceProvider.GetService<IApiClientServer>();
                clientServer.LoadPlugins(assemblies);
                clientServer.Websocket(config.Data.Client.CApi.ApiPort, config.Data.Client.CApi.ApiPassword);
                Logger.Instance.Warning($"client api listen:{config.Data.Client.CApi.ApiPort}");
                Logger.Instance.Warning($"client api password:{config.Data.Client.CApi.ApiPassword}");
            }

            if (config.Data.Client.CApi.WebPort > 0)
            {
                IWebClientServer webServer = serviceProvider.GetService<IWebClientServer>();
                webServer.Start(config.Data.Client.CApi.WebPort, config.Data.Client.CApi.WebRoot);
                Logger.Instance.Warning($"client web listen:{config.Data.Client.CApi.WebPort}");
            }
        }


        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {

        }
        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
