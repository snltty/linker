using linker.libs;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using linker.config;
using linker.plugins.client.args;
using System.Net;
using linker.libs.extends;

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

        public void AddClient(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            if (string.IsNullOrWhiteSpace(config.Data.Client.Name))
            {
                config.Data.Client.Name = Dns.GetHostName().SubStr(0, 12);
            }

            serviceCollection.AddSingleton<SignInArgsTransfer>();

            serviceCollection.AddSingleton<ClientSignInState>();
            serviceCollection.AddSingleton<ClientSignInTransfer>();

        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            LoggerHelper.Instance.Info($"start client");

            LoggerHelper.Instance.Info($"start client signin transfer");
            ClientSignInTransfer clientTransfer = serviceProvider.GetService<ClientSignInTransfer>();
            clientTransfer.SignInTask();
        }


        public void AddServer(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {

        }
        public void UseServer(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
        }
    }
}
