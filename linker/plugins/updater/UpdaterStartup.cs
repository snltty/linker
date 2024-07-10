using linker.config;
using linker.libs;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;

namespace linker.plugins.updater
{
    public sealed class UpdaterStartup : IStartup
    {
        public string Name => "updater";

        public bool Required => false;

        public StartupLevel Level =>  StartupLevel.Normal;

        public string[] Dependent => Array.Empty<string>();

        public StartupLoadType LoadType =>  StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
        }

        public void AddServer(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
        }

        public void UseClient(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {
            foreach (var item in Process.GetProcessesByName("linker.updater"))
            {
                item.Kill();
            }
            //CommandHelper.Execute();
        }

        public void UseServer(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {
        }
    }
}
