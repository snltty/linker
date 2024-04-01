using cmonitor.config;
using cmonitor.plugins.screen.messenger;
using cmonitor.plugins.screen.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.screen
{
    internal class ScreenStartup : IStartup
    {
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ScreenReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IScreen, ScreenWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IScreen, ScreenLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IScreen, ScreenMacOS>();

            serviceCollection.AddSingleton<ScreenClientMessenger>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ScreenServerMessenger>();
            serviceCollection.AddSingleton<ScreenApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
