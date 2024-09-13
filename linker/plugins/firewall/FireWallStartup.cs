using linker.config;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace linker.plugins.firewall
{
    public sealed class FireWallStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Hight9;
        public string Name => "firewall";
        public bool Required => false;
        public string[] Dependent => new string[] { };
        public StartupLoadType LoadType => StartupLoadType.Dependent;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            //linker.libs.FireWallHelper.Write(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
#if DEBUG
#else
            linker.libs.FireWallHelper.Write(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
#endif
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
        }
    }
}
