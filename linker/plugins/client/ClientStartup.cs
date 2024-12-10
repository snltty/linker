using linker.libs;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using linker.config;

namespace linker.plugins.client
{
    /// <summary>
    /// 客户端插件
    /// </summary>
    public sealed class ClientStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Bottom;
        public string Name => "client";
        public bool Required => true;
        public string[] Dependent => new string[] { "messenger", "firewall", "signin", "serialize", "config" };
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {

            serviceCollection.AddSingleton<ClientSignInState>();
            serviceCollection.AddSingleton<ClientSignInTransfer>();

            serviceCollection.AddSingleton<SignInArgsSecretKeyClient>();
            serviceCollection.AddSingleton<SignInArgsGroupPasswordClient>();

            serviceCollection.AddSingleton<ConfigSyncSignInSecretKey>();
            serviceCollection.AddSingleton<ConfigSyncGroupSecretKey>();

            serviceCollection.AddSingleton<ClientConfigTransfer>();
            
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            LoggerHelper.Instance.Info($"start client");

            LoggerHelper.Instance.Info($"start client signin transfer");
            ClientSignInTransfer clientTransfer = serviceProvider.GetService<ClientSignInTransfer>();
            clientTransfer.SignInTask();
        }


        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<SignInArgsSecretKeyServer>();
            serviceCollection.AddSingleton<SignInArgsGroupPasswordServer>();
        }
        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
        }
    }
}
