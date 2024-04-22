using cmonitor.config;
using cmonitor.plugins.volume.messenger;
using cmonitor.plugins.volume.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.volume
{
    public sealed class VolumeStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<VolumeReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IVolume, VolumeWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IVolume, VolumeLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IVolume, VolumeMacOS>();

            serviceCollection.AddSingleton<VolumeClientMessenger>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<VolumeApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
