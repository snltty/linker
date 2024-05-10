using cmonitor.client.report;
using cmonitor.config;
using cmonitor.libs;
using cmonitor.startup;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using cmonitor.client.args;
using cmonitor.client.running;
using cmonitor.client.api;
using cmonitor.client.web;

namespace cmonitor.client
{
    public sealed class ClientStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "client";
        public bool Required => true;
        public string[] Dependent => new string[] { "firewall", "signin", "serialize" };
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<RunningConfig>();

            serviceCollection.AddSingleton<SignInArgsTransfer>();

            serviceCollection.AddSingleton<ClientReportTransfer>();

            serviceCollection.AddSingleton<ClientSignInState>();
            serviceCollection.AddSingleton<ClientSignInTransfer>();

            //内存共享
            ShareMemory shareMemory = new ShareMemory(config.Data.Client.ShareMemoryKey, config.Data.Client.ShareMemoryCount, config.Data.Client.ShareMemorySize);
            serviceCollection.AddSingleton<ShareMemory>((a) => shareMemory);

            serviceCollection.AddSingleton<IApiClientServer, ApiClientServer>();
            serviceCollection.AddSingleton<IWebClientServer, WebClientServer>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            Logger.Instance.Info($"start client");
            Logger.Instance.Info($"server ip {config.Data.Client.ServerEP}");

            Logger.Instance.Info($"start client report transfer");
            ClientReportTransfer report = serviceProvider.GetService<ClientReportTransfer>();
            report.LoadPlugins(assemblies);

            Logger.Instance.Info($"start client share memory");
            ShareMemory shareMemory = serviceProvider.GetService<ShareMemory>();
            shareMemory.InitLocal();
            shareMemory.InitGlobal();
            shareMemory.StartLoop();

            Logger.Instance.Info($"start client signin transfer");
            ClientSignInTransfer clientTransfer = serviceProvider.GetService<ClientSignInTransfer>();


            if (config.Data.Client.ApiPort > 0)
            {
                Logger.Instance.Info($"start client api server");
                IApiClientServer clientServer = serviceProvider.GetService<IApiClientServer>();
                clientServer.LoadPlugins(assemblies);
                clientServer.Websocket(config.Data.Client.ApiPort, config.Data.Client.ApiPassword);
                Logger.Instance.Info($"client api listen:{config.Data.Client.ApiPort}");
                Logger.Instance.Info($"client api password:{config.Data.Client.ApiPassword}");
            }

            if (config.Data.Client.WebPort > 0)
            {
                IWebClientServer webServer = serviceProvider.GetService<IWebClientServer>();
                webServer.Start(config.Data.Client.WebPort, config.Data.Client.WebRoot);
                Logger.Instance.Info($"client web listen:{config.Data.Client.WebPort}");
            }
        }


        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {

        }
        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
