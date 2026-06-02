using linker.libs.web;
using linker.tunnel;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.pcp
{
    public static class Entry
    {
        public static ServiceCollection AddPcpClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<PcpClientMessenger>();
            serviceCollection.AddSingleton<PcpApiController>();

            serviceCollection.AddSingleton<TransportPcp>();
            serviceCollection.AddSingleton<TunnelWanPortProtocolPcp>();

            serviceCollection.AddSingleton<PcpNodeTransfer>();

            return serviceCollection;
        }
        public static ServiceProvider UsePcpClient(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<PcpClientMessenger>() });

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<PcpApiController>() });

            TunnelTransfer tunnelTransfer = serviceProvider.GetService<TunnelTransfer>();
            tunnelTransfer.AddTransport(serviceProvider.GetService<TransportPcp>());
            tunnelTransfer.AddProtocol(serviceProvider.GetService<TunnelWanPortProtocolPcp>());

            return serviceProvider;
        }


        public static ServiceCollection AddPcpServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<PcpServerMessenger>();
            return serviceCollection;
        }
        public static ServiceProvider UsePcpServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<PcpServerMessenger>() });

            return serviceProvider;
        }
    }
}
