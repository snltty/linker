using linker.config;
using linker.startup;
using linker.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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
        public string[] Dependent => new string[] {"messenger", "serialize", "firewall", "signin", "config"};
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<TcpServer>();
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            LoggerHelper.Instance.Info($"start server");
            try
            {
                //服务
                TcpServer tcpServer = serviceProvider.GetService<TcpServer>();
                if (config.Data.Server.ServicePort > 0)
                {
                    tcpServer.Start(config.Data.Server.ServicePort);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            LoggerHelper.Instance.Info($"server listen:{config.Data.Server.ServicePort}");

        }
    }
}
