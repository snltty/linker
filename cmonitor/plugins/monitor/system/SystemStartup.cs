using cmonitor.config;
using cmonitor.plugins.system.messenger;
using cmonitor.plugins.system.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.system
{
    internal sealed class SystemStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "system";

        public bool Required => false;

        public string[] Dependent => new string[] { };

        public StartupLoadType LoadType => StartupLoadType.Normal;


        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<SystemReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<ISystem, SystemWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<ISystem, SystemLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<ISystem, SystemMacOS>();

            serviceCollection.AddSingleton<SystemClientMessenger>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<SystemApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
