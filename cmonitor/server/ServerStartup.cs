using cmonitor.config;
using cmonitor.server.api;
using cmonitor.server.ruleConfig;
using cmonitor.server.web;
using cmonitor.startup;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.server
{
    public sealed class ServerStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "server";
        public bool Required => true;
        public string[] Dependent => new string[] { "serialize", "firewall", "signin", "devices", "modes", "rule", "report", "share" };
        public StartupLoadType LoadType => StartupLoadType.Normal;

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


            serviceCollection.AddSingleton<IWebServerServer, WebServerServer>();
            serviceCollection.AddSingleton<IApiServerServer, ApiServerServer>();
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
            tcpServer.Start(config.Data.Server.ServicePort);
            Logger.Instance.Info($"server listen:{config.Data.Server.ServicePort}");

            if (config.Data.Server.WebPort > 0)
            {
                IWebServerServer webServer = serviceProvider.GetService<IWebServerServer>();
                webServer.Start(config.Data.Server.WebPort, config.Data.Server.WebRoot);
                Logger.Instance.Info($"server web listen:{config.Data.Server.WebPort}");
            }
            if (config.Data.Server.ApiPort > 0)
            {
                Logger.Instance.Info($"start server api ");
                IApiServerServer clientServer = serviceProvider.GetService<IApiServerServer>();
                clientServer.LoadPlugins(assemblies);
                clientServer.Websocket(config.Data.Server.ApiPort, config.Data.Server.ApiPassword);
                Logger.Instance.Info($"server api listen:{config.Data.Server.ApiPort}");
                Logger.Instance.Info($"server api password:{config.Data.Server.ApiPassword}");
            }
        }
    }
}
