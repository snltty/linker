using linker.libs;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using linker.client.args;
using linker.client.config;
using linker.config;

namespace linker.client
{
    /// <summary>
    /// 客户端插件
    /// </summary>
    public sealed class ClientStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Bottom;
        public string Name => "client";
        public bool Required => true;
        public string[] Dependent => new string[] { "firewall", "signin", "serialize" };
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<RunningConfig>();

            serviceCollection.AddSingleton<SignInArgsTransfer>();

            serviceCollection.AddSingleton<ClientSignInState>();
            serviceCollection.AddSingleton<ClientSignInTransfer>();

        }

        public void UseClient(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {
            LoggerHelper.Instance.Info($"start client");

            LoggerHelper.Instance.Info($"start client signin transfer");
            ClientSignInTransfer clientTransfer = serviceProvider.GetService<ClientSignInTransfer>();
            clientTransfer.SignInTask();
        }


        public void AddServer(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {

        }
        public void UseServer(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {
        }
    }
}
