using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.pcp
{
    public static class Entry
    {
        public static ServiceCollection AddRelayClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<PcpTransfer>();
            serviceCollection.AddSingleton<PcpClientMessenger>();
            return serviceCollection;
        }
        public static ServiceProvider UseRelayClient(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<PcpClientMessenger>() });
            return serviceProvider;
        }


        public static ServiceCollection AddRelayServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<PcpServerMessenger>();
            return serviceCollection;
        }
        public static ServiceProvider UseRelayServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<PcpServerMessenger>() });

            return serviceProvider;
        }
    }
}
