using linker.libs.web;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
namespace linker.messenger.channel
{
    public static class Entry
    {
        public static ServiceCollection AddChannelClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ChannelApiController>();
            serviceCollection.AddSingleton<ChannelConnectionCaching>();

            return serviceCollection;
        }
        public static ServiceProvider UseChannelClient(this ServiceProvider serviceProvider, JsonDocument json = default)
        {
            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<ChannelApiController>() });

            return serviceProvider;
        }
    }
}
