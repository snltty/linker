using linker.config;
using linker.plugins.forward.messenger;
using linker.plugins.forward.proxy;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace linker.plugins.forward
{
    public sealed class ForwardStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "forward";
        public bool Required => false;
        public string[] Dependent => new string[] { "relay", "tunnel" };

        public StartupLoadType LoadType => StartupLoadType.Normal;


        public void AddClient(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ForwardClientApiController>();
            serviceCollection.AddSingleton<ForwardTransfer>();
            serviceCollection.AddSingleton<ForwardProxy>();

            serviceCollection.AddSingleton<ForwardClientMessenger>();

        }

        public void AddServer(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ForwardServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {
            ForwardTransfer forwardTransfer = serviceProvider.GetService<ForwardTransfer>();
        }

        public void UseServer(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {
        }
    }
}
