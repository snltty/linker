using linker.config;
using linker.startup;
using linker.libs;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.server
{
    /// <summary>
    /// 服务端插件
    /// </summary>
    public sealed class ServerStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "server";
        public bool Required => true;
        public string[] Dependent => new string[] { "messenger", "serialize", "firewall", "signin", "config" };
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<TcpServer>();
            serviceCollection.AddSingleton<ServerConfigTransfer>();
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
            ServerConfigTransfer serverConfigTransfer = serviceProvider.GetService<ServerConfigTransfer>();
            LoggerHelper.Instance.Info($"start server");
            try
            {
                //服务
                TcpServer tcpServer = serviceProvider.GetService<TcpServer>();
                tcpServer.Start(serverConfigTransfer.Port);
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            LoggerHelper.Instance.Warning($"server listen:{serverConfigTransfer.Port}");

        }
    }
}
