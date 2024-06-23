using link.config;
using link.plugins.tuntap.messenger;
using link.plugins.tuntap.proxy;
using link.plugins.tuntap.vea;
using link.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace link.plugins.tuntap
{
    /// <summary>
    /// 虚拟网卡组网插件
    /// </summary>
    public sealed class TuntapStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "tuntap";
        public bool Required => false;
        public string[] Dependent => new string[] { "relay", "tunnel" };

        public StartupLoadType LoadType => StartupLoadType.Normal;


        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            //不同平台下的虚拟网卡
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
