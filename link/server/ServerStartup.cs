using link.config;
using link.startup;
using link.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace link.server
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

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<MessengerSender>();
            serviceCollection.AddSingleton<MessengerResolver>();
            serviceCollection.AddSingleton<TcpServer>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<MessengerSender>();
            serviceCollection.AddSingleton<MessengerResolver>();
            serviceCollection.AddSingleton<TcpServer>();

        }


        private bool loaded = false;
        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            if (loaded == false)
            {
                MessengerResolver messengerResolver = serviceProvider.GetService<MessengerResolver>();
                messengerResolver.LoadMessenger(assemblies);
                loaded = true;
            }
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            if (loaded == false)
            {
                MessengerResolver messengerResolver = serviceProvider.GetService<MessengerResolver>();
                messengerResolver.LoadMessenger(assemblies);
                loaded = true;
            }

            Logger.Instance.Info($"start server");
            try
            {
                //服务
                TcpServer tcpServer = serviceProvider.GetService<TcpServer>();
                tcpServer.Init(config.Data.Server.Certificate, config.Data.Server.Password);
                tcpServer.Start(config.Data.Server.ServicePort);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            Logger.Instance.Info($"server listen:{config.Data.Server.ServicePort}");

        }
    }
}
