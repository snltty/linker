using linker.config;
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

            serviceCollection.AddSingleton<MessengerSender>();
            serviceCollection.AddSingleton<MessengerResolver>();
            serviceCollection.AddSingleton<MessengerFlow>();

        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<MessengerSender>();
            serviceCollection.AddSingleton<MessengerResolver>();
            serviceCollection.AddSingleton<MessengerFlow>();
        }


        private bool loaded = false;
        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            if (loaded == false)
            {
                loaded = true;

                MessengerResolver messengerResolver = serviceProvider.GetService<MessengerResolver>();
                messengerResolver.LoadMessenger();
                messengerResolver.Init(config.Data.Client.SSL.File, config.Data.Client.SSL.Password);

            }
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
            if (loaded == false)
            {
                loaded = true;

                MessengerResolver messengerResolver = serviceProvider.GetService<MessengerResolver>();
                messengerResolver.LoadMessenger();
                messengerResolver.Init(config.Data.Server.SSL.File, config.Data.Server.SSL.Password);

            }
        }
    }
}
