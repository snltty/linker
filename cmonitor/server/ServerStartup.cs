using cmonitor.config;
using cmonitor.server.ruleConfig;
using cmonitor.startup;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.server
{
    public sealed class ServerStartup : IStartup
    {
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<MessengerSender>();
            serviceCollection.AddSingleton<MessengerResolver>();
            serviceCollection.AddSingleton<TcpServer>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<IRuleConfig, RuleConfigWindows>();
            // if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IRuleConfig, RuleConfigWindows>();
            // else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IRuleConfig, RuleConfigLinux>();
            // else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IRuleConfig, RuleConfigMacOS>();

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
            //服务
            TcpServer tcpServer = serviceProvider.GetService<TcpServer>();
            tcpServer.Start();
            Logger.Instance.Info($"server listen:{config.Server.ServicePort}");
        }
    }
}
