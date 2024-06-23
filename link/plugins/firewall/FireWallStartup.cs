using link.config;
using link.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace link.plugins.firewall
{
    public sealed class FireWallStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Hight9;
        public string Name => "firewall";
        public bool Required => false;
        public string[] Dependent => new string[] { };
        public StartupLoadType LoadType => StartupLoadType.Dependent;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
#if DEBUG
#else
            link.libs.FireWallHelper.Write(Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "./plugins/firewall");
#endif
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
