using cmonitor.config;
using cmonitor.plugins.keyboard.messenger;
using cmonitor.plugins.keyboard.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.keyboard
{
    public sealed class KeyboardStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<KeyboardReport>();
            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IKeyboard, KeyboardWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IKeyboard, KeyboardLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IKeyboard, KeyboardMacOS>();

            serviceCollection.AddSingleton<KeyboardClientMessenger>();
            
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<KeyboardApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
