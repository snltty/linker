using linker.libs;
using linker.libs.web;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.api
{
    public static class Entry
    {
        public static ServiceCollection AddApiClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IWebServer, WebServer>();

            serviceCollection.AddSingleton<IWebServerFileReader, WebServerFileReader>();

            return serviceCollection;
        }
        public static ServiceProvider UseApiClient(this ServiceProvider serviceProvider)
        {
            IApiStore apiStore = serviceProvider.GetService<IApiStore>();
            IAccessStore accessStore = serviceProvider.GetService<IAccessStore>();

            if (apiStore.Info.WebPort > 0 && accessStore.HasAccess(AccessValue.Web))
            {
                LoggerHelper.Instance.Info($"start client web");
                IWebServer webServer = serviceProvider.GetService<IWebServer>();
                webServer.Start(apiStore.Info.WebPort, apiStore.Info.WebRoot, apiStore.Info.ApiPassword);
                LoggerHelper.Instance.Warning($"client web listen:{apiStore.Info.WebPort}");
            }
            return serviceProvider;
        }
    }
}
