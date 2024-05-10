using cmonitor.config;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.elevated
{
    public sealed class ElevatedStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Hight9;
        public string Name => "elevated";
        public bool Required => false;
        public string[] Dependent => new string[] { "ntrights" };
        public StartupLoadType LoadType => StartupLoadType.Dependent;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
#if RELEASE
            if (common.libs.winapis.Win32Interop.GetCommandLine().Contains("--elevated") == false)
            {
                common.libs.winapis.Win32Interop.RelaunchElevated();
            }
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
