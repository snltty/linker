using cmonitor.config;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.startup
{
    public static class StartupTransfer
    {
        static List<IStartup> startups;
        public static void Init()
        {
            startups = ReflectionHelper.GetInterfaceSchieves(typeof(IStartup)).Select(c => Activator.CreateInstance(c) as IStartup).ToList();
        }

        public static void Add(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            foreach (var startup in startups)
            {
                if (config.Common.Modes.Contains("client"))
                    startup.AddClient(serviceCollection, config, assemblies);
                if (config.Common.Modes.Contains("server"))
                    startup.AddServer(serviceCollection, config, assemblies);
            }
        }
        public static void Use(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            foreach (var startup in startups)
            {
                if (config.Common.Modes.Contains("client"))
                    startup.UseClient(serviceProvider, config, assemblies);
                if (config.Common.Modes.Contains("server"))
                    startup.UseServer(serviceProvider, config, assemblies);
            }
        }
    }
}
