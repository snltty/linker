using cmonitor.config;
using cmonitor.plugins.active.db;
using cmonitor.plugins.active.messenger;
using cmonitor.plugins.active.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.active
{
    public sealed class ActiveStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "active";
        public bool Required => false;
        public string[] Dependent => Array.Empty<string>();
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ActiveWindowReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IActiveWindow, ActiveWindowWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IActiveWindow, ActiveWindowLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IActiveWindow, ActiveWindowMacOS>();

            serviceCollection.AddSingleton<IActiveWindowDB, ActiveWindowDB>();

            serviceCollection.AddSingleton<ActiveClientMessenger>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ActiveApiController>();
            serviceCollection.AddSingleton<IActiveWindowDB, ActiveWindowDB>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {

        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
