using cmonitor.config;
using cmonitor.plugins.hijack.messenger;
using cmonitor.plugins.hijack.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.hijack
{
    public sealed class HijackStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<HijackReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IHijack, HijackWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IHijack, HijackLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IHijack, HijackMacOS>();

            serviceCollection.AddSingleton<HijackClientMessenger>();

        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<HijackApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
