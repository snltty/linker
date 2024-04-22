using cmonitor.config;
using cmonitor.plugins.notify.messenger;
using cmonitor.plugins.notify.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.notify
{
    public sealed class NotifyStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<NotifyReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<INotify, NotifyWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<INotify, NotifyLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<INotify, NotifyMacOS>();

            serviceCollection.AddSingleton<NotifyClientMessenger>();

        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<NotifyApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
