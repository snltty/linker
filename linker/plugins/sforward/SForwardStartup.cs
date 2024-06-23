using linker.config;
using linker.plugins.sforward.config;
using linker.plugins.sforward.messenger;
using linker.plugins.sforward.validator;
using linker.startup;
using linker.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace linker.plugins.sforward
{
    public sealed class SForwardStartup : IStartup
    {
        public string Name => "sforward";

        public bool Required => false;

        public StartupLevel Level => StartupLevel.Normal;

        public string[] Dependent => Array.Empty<string>();

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            Add(serviceCollection, config, assemblies);
            serviceCollection.AddSingleton<SForwardClientApiController>();
            serviceCollection.AddSingleton<SForwardTransfer>();
            serviceCollection.AddSingleton<SForwardClientMessenger>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            Add(serviceCollection, config, assemblies);
            serviceCollection.AddSingleton<SForwardServerMessenger>();
            serviceCollection.AddSingleton<ISForwardServerCahing, SForwardServerCahing>();
            serviceCollection.AddSingleton<IValidator, Validator>();

        }

        bool added = false;
        private void Add(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            if (added == false)
            {
                added = true;
                serviceCollection.AddSingleton<SForwardProxy>();
            }
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            SForwardTransfer forwardTransfer = serviceProvider.GetService<SForwardTransfer>();
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            SForwardProxy sForwardProxy = serviceProvider.GetService<SForwardProxy>();
            if (config.Data.Server.SForward.WebPort > 0)
            {
                sForwardProxy.Start(config.Data.Server.SForward.WebPort, true);
                Logger.Instance.Info($"listen server forward web in {config.Data.Server.SForward.WebPort}");
            }
            Logger.Instance.Info($"listen server forward tunnel in {string.Join("-", config.Data.Server.SForward.TunnelPortRange)}");
        }
    }
}
