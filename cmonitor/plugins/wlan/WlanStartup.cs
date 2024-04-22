using cmonitor.config;
using cmonitor.plugins.wlan.messenger;
using cmonitor.plugins.wlan.report;
using cmonitor.startup;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.wlan
{
    public sealed class WlanStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<WlanReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IWlan, WlanWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IWlan, WlanLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IWlan, WlanMacOS>();

            serviceCollection.AddSingleton<WlanClientMessenger>();

        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<WlanApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
