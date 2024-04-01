using cmonitor.config;
using cmonitor.plugins.viewer.messenger;
using cmonitor.plugins.viewer.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.viewer
{
    public sealed class ViewerStartup : IStartup
    {
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ViewerReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IViewer, ViewerWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IViewer, ViewerLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IViewer, ViewerMacOS>();

            serviceCollection.AddSingleton<ViewerClientMessenger>();

        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ViewerServerMessenger>();
            serviceCollection.AddSingleton<ViewerApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
