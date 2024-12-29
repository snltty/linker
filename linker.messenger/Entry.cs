using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger
{
    public static class Entry
    {
        public static ServiceCollection AddMessenger(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IMessengerSender, MessengerSender>();
            serviceCollection.AddSingleton<IMessengerResolver, MessengerResolver>();
            serviceCollection.AddSingleton<MessengerResolverResolver>();
            return serviceCollection;
        }
        public static ServiceProvider UseMessenger(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();

            ResolverTransfer resolverTransfer = serviceProvider.GetService<ResolverTransfer>();
            resolverTransfer.AddResolvers(new List<IResolver> { serviceProvider.GetService<MessengerResolverResolver>() });

            return serviceProvider;
        }

    }
}
