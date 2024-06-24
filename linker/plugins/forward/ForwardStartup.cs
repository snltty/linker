using Linker.Config;
using Linker.Plugins.Forward.Proxy;
using Linker.Startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Linker.Plugins.Forward
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
            
        }

        public void AddServer(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
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
