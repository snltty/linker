using link.config;
using link.libs;
using link.startup;
using link.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using link.client.args;
using link.client.config;

namespace link.client
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

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<RunningConfig>();

            serviceCollection.AddSingleton<SignInArgsTransfer>();

            serviceCollection.AddSingleton<ClientSignInState>();
            serviceCollection.AddSingleton<ClientSignInTransfer>();

        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            Logger.Instance.Info($"start client");

            Logger.Instance.Info($"start client signin transfer");
            ClientSignInTransfer clientTransfer = serviceProvider.GetService<ClientSignInTransfer>();
            clientTransfer.SignInTask();
        }


        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {

        }
        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
