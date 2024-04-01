using cmonitor.config;
using cmonitor.plugins.wallpaper.messenger;
using cmonitor.plugins.wallpaper.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.wallpaper
{
    public sealed class WallpaperStartup : IStartup
    {
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<WallpaperReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IWallpaper, WallpaperWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IWallpaper, WallpaperLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IWallpaper, WallpaperMacOS>();

            serviceCollection.AddSingleton<WallpaperClientMessenger>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<WallpaperApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
