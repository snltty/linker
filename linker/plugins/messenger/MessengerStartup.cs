using linker.config;
using linker.plugins.resolver;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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

        public void AddClient(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {

            serviceCollection.AddSingleton<MessengerSender>();
            serviceCollection.AddSingleton<MessengerResolver>();
            serviceCollection.AddSingleton<MessengerFlow>();
            
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<MessengerSender>();
            serviceCollection.AddSingleton<MessengerResolver>();
            serviceCollection.AddSingleton<MessengerFlow>();
        }


        private bool loaded = false;
        public void UseClient(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            if (loaded == false)
            {
                loaded = true;

                MessengerResolver messengerResolver = serviceProvider.GetService<MessengerResolver>();
                messengerResolver.LoadMessenger(assemblies);
                messengerResolver.Init(config.Data.Client.Certificate, config.Data.Client.Password);

            }
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            if (loaded == false)
            {
                loaded = true;

                MessengerResolver messengerResolver = serviceProvider.GetService<MessengerResolver>();
                messengerResolver.LoadMessenger(assemblies);
                messengerResolver.Init(config.Data.Server.Certificate, config.Data.Server.Password);

            }
        }
    }
}
