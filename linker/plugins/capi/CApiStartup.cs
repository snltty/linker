using linker.config;
using linker.startup;
using linker.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace linker.plugins.capi
{
    public sealed class CApiStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "capi";
        public bool Required => false;
        public string[] Dependent => new string[] { };
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<IApiClientServer, ApiClientServer>();
            serviceCollection.AddSingleton<IWebClientServer, WebClientServer>();
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            if (config.Data.Client.CApi.ApiPort > 0)
            {
                LoggerHelper.Instance.Info($"start client api server");
                IApiClientServer clientServer = serviceProvider.GetService<IApiClientServer>();
                clientServer.LoadPlugins(assemblies);
                clientServer.Websocket(config.Data.Client.CApi.ApiPort, config.Data.Client.CApi.ApiPassword);
                LoggerHelper.Instance.Warning($"client api listen:{config.Data.Client.CApi.ApiPort}");
                if (config.Data.Client.HasAccess(ClientApiAccess.Api))
                    LoggerHelper.Instance.Warning($"client api password:{config.Data.Client.CApi.ApiPassword}");
            }

            if (config.Data.Client.CApi.WebPort > 0 && config.Data.Client.HasAccess(ClientApiAccess.Web))
            {
                IWebClientServer webServer = serviceProvider.GetService<IWebClientServer>();
                webServer.Start(config.Data.Client.CApi.WebPort, config.Data.Client.CApi.WebRoot);
                LoggerHelper.Instance.Warning($"client web listen:{config.Data.Client.CApi.WebPort}");
            }
        }


        public void AddServer(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {

        }
        public void UseServer(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
        }
    }
}
