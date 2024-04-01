using cmonitor.config;
using cmonitor.plugins.active.messenger;
using cmonitor.plugins.active.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.active
{
    public sealed class ActiveStartup : IStartup
    {
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ActiveWindowReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IActiveWindow, ActiveWindowWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IActiveWindow, ActiveWindowLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IActiveWindow, ActiveWindowMacOS>();

            serviceCollection.AddSingleton<ActiveClientMessenger>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ActiveApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {

        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
