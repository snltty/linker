using cmonitor.config;
using cmonitor.plugins.llock.messenger;
using cmonitor.plugins.llock.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.llock
{
    public sealed class LLockStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "llock";

        public bool Required => false;

        public string[] Dependent => new string[] { };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<LLockReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<ILLock, LLockWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<ILLock, LLockLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<ILLock, LLockMacOS>();

            serviceCollection.AddSingleton<LLockClientMessenger>();

        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<LLockApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
