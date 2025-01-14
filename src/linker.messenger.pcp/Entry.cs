using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.pcp
{
    public static class Entry
    {
        public static ServiceCollection AddPcpClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<PcpTransfer>();
            serviceCollection.AddSingleton<PcpClientMessenger>();
            return serviceCollection;
        }
        public static ServiceProvider UsePcpClient(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<PcpClientMessenger>() });
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
