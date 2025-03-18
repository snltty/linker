using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.decenter
{
    public static class Entry
    {
        public static ServiceCollection AddDecenterClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<DecenterClientTransfer>();

            serviceCollection.AddSingleton<DecenterClientMessenger>();

            return serviceCollection;
        }
        public static ServiceProvider UseDecenterClient(this ServiceProvider serviceProvider)
        {
            DecenterClientTransfer decenterClientTransfer = serviceProvider.GetService<DecenterClientTransfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<DecenterClientMessenger>() });

            return serviceProvider;
        }

        public static ServiceCollection AddDecenterServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<DecenterServerMessenger>();

            return serviceCollection;
        }
        public static ServiceProvider UseDecenterServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<DecenterServerMessenger>() });

            return serviceProvider;
        }
    }
}
