using linker.config;
using linker.plugins.sforward.config;
using linker.plugins.sforward.messenger;
using linker.plugins.sforward.validator;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using linker.plugins.sforward.proxy;
using linker.libs;

namespace linker.plugins.sforward
{
    public sealed class SForwardStartup : IStartup
    {
        public string Name => "sforward";

        public bool Required => false;

        public StartupLevel Level => StartupLevel.Normal;

        public string[] Dependent => new string[] { "messenger", "signin", "serialize", "config" };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            Add(serviceCollection, config);
            serviceCollection.AddSingleton<SForwardClientApiController>();
            serviceCollection.AddSingleton<SForwardTransfer>();
            serviceCollection.AddSingleton<SForwardClientMessenger>();

            serviceCollection.AddSingleton<SForwardConfigSyncSecretKey>();

        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            Add(serviceCollection, config);
            serviceCollection.AddSingleton<SForwardServerMessenger>();
            serviceCollection.AddSingleton<ISForwardServerCahing, SForwardServerCahing>();
            serviceCollection.AddSingleton<ISForwardValidator, Validator>();

            serviceCollection.AddSingleton<SForwardServerConfigTransfer>();
        }

        bool added = false;
        private void Add(ServiceCollection serviceCollection, FileConfig config)
        {
            if (added == false)
            {
                added = true;
                serviceCollection.AddSingleton<SForwardProxy, SForwardProxy>();
            }
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            SForwardTransfer forwardTransfer = serviceProvider.GetService<SForwardTransfer>();
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
            SForwardProxy sForwardProxy = serviceProvider.GetService<SForwardProxy>();
            SForwardServerConfigTransfer sForwardServerConfigTransfer = serviceProvider.GetService<SForwardServerConfigTransfer>();
            if (sForwardServerConfigTransfer.WebPort > 0)
            {
                sForwardProxy.Start(sForwardServerConfigTransfer.WebPort, true, sForwardServerConfigTransfer.BufferSize,string.Empty);
                LoggerHelper.Instance.Warning($"listen server forward web in {sForwardServerConfigTransfer.WebPort}");
            }
            LoggerHelper.Instance.Warning($"listen server forward tunnel in {string.Join("-", sForwardServerConfigTransfer.TunnelPortRange)}");

        }
    }

}
