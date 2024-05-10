using cmonitor.config;
using cmonitor.plugins.snatch.messenger;
using cmonitor.plugins.snatch.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.snatch
{
    public sealed class SnatchStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "snatch";

        public bool Required => false;

        public string[] Dependent => new string[] { };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<SnatchReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<ISnatch, SnatchWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<ISnatch, SnatchLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<ISnatch, SnatchMacOS>();

            serviceCollection.AddSingleton<SnatchClientMessenger>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<SnatchServerMessenger>();
            serviceCollection.AddSingleton<SnatchApiController>();
            serviceCollection.AddSingleton<ISnatachCaching, SnatachCachingMemory>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
