using linker.config;
using linker.startup;
using linker.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace linker.server
{
    /// <summary>
    /// 服务端插件
    /// </summary>
    public sealed class ServerStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "server";
        public bool Required => true;
        public string[] Dependent => new string[] { "serialize", "firewall", "signin" };
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<MessengerSender>();
            serviceCollection.AddSingleton<MessengerResolver>();
            serviceCollection.AddSingleton<TcpServer>();
        }

        public void AddServer(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<MessengerSender>();
            serviceCollection.AddSingleton<MessengerResolver>();
            serviceCollection.AddSingleton<TcpServer>();

        }


        private bool loaded = false;
        public void UseClient(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {
            if (loaded == false)
            {
                MessengerResolver messengerResolver = serviceProvider.GetService<MessengerResolver>();
                messengerResolver.LoadMessenger(assemblies);
                loaded = true;
            }
        }

        public void UseServer(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {
            if (loaded == false)
            {
                MessengerResolver messengerResolver = serviceProvider.GetService<MessengerResolver>();
                messengerResolver.LoadMessenger(assemblies);
                loaded = true;
            }

            LoggerHelper.Instance.Info($"start server");
            try
            {
                //服务
                TcpServer tcpServer = serviceProvider.GetService<TcpServer>();
                tcpServer.Init(config.Data.Server.Certificate, config.Data.Server.Password);
                tcpServer.Start(config.Data.Server.ServicePort);
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            LoggerHelper.Instance.Info($"server listen:{config.Data.Server.ServicePort}");

        }
    }
}
