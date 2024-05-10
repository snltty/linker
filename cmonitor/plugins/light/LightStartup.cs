using cmonitor.config;
using cmonitor.plugins.light.messenger;
using cmonitor.plugins.light.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.light
{
    public sealed class LightStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;

        public string Name => "light";

        public bool Required => false;

        public string[] Dependent => new string[] { };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<LightReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<ILight, LightWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<ILight, LightLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<ILight, LightMacOS>();

            serviceCollection.AddSingleton<LightClientMessenger>();

        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<LightApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
