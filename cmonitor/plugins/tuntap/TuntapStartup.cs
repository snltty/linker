using cmonitor.config;
using cmonitor.plugins.tuntap;
using cmonitor.plugins.tuntap.messenger;
using cmonitor.plugins.tuntap.proxy;
using cmonitor.plugins.tuntap.vea;
using cmonitor.startup;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Reflection;

namespace cmonitor.plugins.viewer
{
    public sealed class TuntapStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "tuntap";
        public bool Required => false;
        public string[] Dependent => new string[] { "relay", "tunnel" };

        public StartupLoadType LoadType => StartupLoadType.Normal;


        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<ITuntapVea, TuntapVeaWindows>();
            if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<ITuntapVea, TuntapVeaLinux>();
            if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<ITuntapVea, TuntapVeaMacOs>();

            serviceCollection.AddSingleton<TuntapClientApiController>();
            serviceCollection.AddSingleton<TuntapTransfer>();
            serviceCollection.AddSingleton<TuntapProxy>();

            serviceCollection.AddSingleton<TuntapClientMessenger>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<TuntapServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            TuntapProxy tuntapProxy = serviceProvider.GetService<TuntapProxy>();
            TuntapTransfer tuntapTransfer = serviceProvider.GetService<TuntapTransfer>();
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
