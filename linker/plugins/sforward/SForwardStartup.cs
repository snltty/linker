using linker.config;
using linker.plugins.sforward.config;
using linker.plugins.sforward.messenger;
using linker.plugins.sforward.validator;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using linker.plugins.sforward.proxy;
using linker.libs;
using linker.tun;

namespace linker.plugins.sforward
{
    public sealed class SForwardStartup : IStartup
    {
        public string Name => "sforward";

        public bool Required => false;

        public StartupLevel Level => StartupLevel.Normal;

        public string[] Dependent => new string[] { "messenger", "signin", "serialize", "config" };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            Add(serviceCollection, config, assemblies);
            serviceCollection.AddSingleton<SForwardClientApiController>();
            serviceCollection.AddSingleton<SForwardTransfer>();
            serviceCollection.AddSingleton<SForwardClientMessenger>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            Add(serviceCollection, config, assemblies);
            serviceCollection.AddSingleton<SForwardServerMessenger>();
            serviceCollection.AddSingleton<ISForwardServerCahing, SForwardServerCahing>();
            serviceCollection.AddSingleton<ISForwardValidator, Validator>();

        }

        bool added = false;
        private void Add(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            if (added == false)
            {
                added = true;
                serviceCollection.AddSingleton<SForwardProxy>();
            }
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            SForwardTransfer forwardTransfer = serviceProvider.GetService<SForwardTransfer>();
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            SForwardProxy sForwardProxy = serviceProvider.GetService<SForwardProxy>();
            if (config.Data.Server.SForward.WebPort > 0)
            {
                sForwardProxy.Start(config.Data.Server.SForward.WebPort, true, config.Data.Server.SForward.BufferSize);
                LoggerHelper.Instance.Info($"listen server forward web in {config.Data.Server.SForward.WebPort}");
            }
            LoggerHelper.Instance.Info($"listen server forward tunnel in {string.Join("-", config.Data.Server.SForward.TunnelPortRange)}");

        }
    }

}
