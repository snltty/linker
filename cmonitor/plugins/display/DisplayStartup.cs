using cmonitor.config;
using cmonitor.plugins.display.messenger;
using cmonitor.plugins.display.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.display
{
    public sealed class DisplayStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<DisplayReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IDisplay, DisplayWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IDisplay, DisplayLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IDisplay, DisplayMacOS>();
            serviceCollection.AddSingleton<DisplayClientMessenger>();
           
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<DisplayApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
