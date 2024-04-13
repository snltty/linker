using cmonitor.config;
using cmonitor.plugins.viewer.messenger;
using cmonitor.plugins.viewer.proxy;
using cmonitor.plugins.viewer.report;
using cmonitor.startup;
using common.libs;
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


            serviceCollection.AddSingleton<ViewerProxySignInArgs>();
            serviceCollection.AddSingleton<ViewerProxyClient>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            
            serviceCollection.AddSingleton<ViewerProxyCaching>();
            serviceCollection.AddSingleton<ViewerServerMessenger>();
            serviceCollection.AddSingleton<ViewerApiController>();
            serviceCollection.AddSingleton<ViewerProxyServer>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            Logger.Instance.Info($"use viewer proxy server in client mode");
            ViewerProxyClient viewerProxyServer = serviceProvider.GetService<ViewerProxyClient>();
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            Logger.Instance.Info($"use viewer proxy server in server mode");
            ViewerProxyServer viewerProxyServer = serviceProvider.GetService<ViewerProxyServer>();
        }
    }
}
