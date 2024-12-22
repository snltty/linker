using linker.config;
using linker.messenger;
using linker.plugins.client;
using linker.plugins.server;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.messenger
{
    /// <summary>
    /// 服务端插件
    /// </summary>
    public sealed class MessengerStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "messenger";
        public bool Required => true;
        public string[] Dependent => new string[] { };
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {

            serviceCollection.AddSingleton<IMessengerSender, MessengerSender>();
            serviceCollection.AddSingleton<IMessengerResolver, MessengerResolver>();
            serviceCollection.AddSingleton<MessengerResolverResolver>();

            serviceCollection.AddSingleton<MessengerResolverTypesLoader>();


        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<IMessengerSender, MessengerSender>();
            serviceCollection.AddSingleton<IMessengerResolver, MessengerResolver>();
            serviceCollection.AddSingleton<MessengerResolverResolver>();
            serviceCollection.AddSingleton<MessengerResolverTypesLoader>();
        }


        private bool loaded = false;
        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            if (loaded == false)
            {
                loaded = true;

                IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
                ClientConfigTransfer clientConfigTransfer = serviceProvider.GetService<ClientConfigTransfer>();
                messengerResolver.Initialize(clientConfigTransfer.Certificate);

                MessengerResolverTypesLoader messengerResolverTypesLoader = serviceProvider.GetService<MessengerResolverTypesLoader>();

            }
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
            if (loaded == false)
            {
                loaded = true;

                ServerConfigTransfer serverConfigTransfer = serviceProvider.GetService<ServerConfigTransfer>();

                IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
                messengerResolver.Initialize(serverConfigTransfer.SSL.File, serverConfigTransfer.SSL.Password);

                MessengerResolverTypesLoader messengerResolverTypesLoader = serviceProvider.GetService<MessengerResolverTypesLoader>();
            }
        }
    }
}
