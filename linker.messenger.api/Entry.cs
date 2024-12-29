using linker.libs;
using linker.libs.web;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.api
{
    public static class Entry
    {
        public static ServiceCollection AddApiClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IApiServer, ApiServer>();
            serviceCollection.AddSingleton<IWebServer, WebServer>();
            return serviceCollection;
        }
        public static ServiceProvider UseApiClient(this ServiceProvider serviceProvider)
        {
            IApiStore apiStore = serviceProvider.GetService<IApiStore>();
            if (apiStore.Info.ApiPort > 0)
            {
                LoggerHelper.Instance.Info($"start client api server");
                IApiServer server = serviceProvider.GetService<IApiServer>();
                server.Websocket(apiStore.Info.ApiPort, apiStore.Info.ApiPassword);
                LoggerHelper.Instance.Warning($"client api listen:{apiStore.Info.ApiPort}");
                LoggerHelper.Instance.Warning($"client api password:{apiStore.Info.ApiPassword}");
            }

            if (apiStore.Info.WebPort > 0)
            {
                IWebServer webServer = serviceProvider.GetService<IWebServer>();
                webServer.Start(apiStore.Info.WebPort, apiStore.Info.WebRoot);
                LoggerHelper.Instance.Warning($"client web listen:{apiStore.Info.WebPort}");
            }
            return serviceProvider;
        }
    }
}
