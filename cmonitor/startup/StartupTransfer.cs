using cmonitor.config;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.startup
{
    public static class StartupTransfer
    {
        static List<IStartup> startups;
        public static void Init(Config config, Assembly[] assemblies)
        {
            var types = ReflectionHelper.GetInterfaceSchieves(assemblies, typeof(IStartup));
            types = config.Data.Common.PluginContains(types);
            startups = types.Select(c => Activator.CreateInstance(c) as IStartup).OrderByDescending(c => c.Level).ToList();

            Logger.Instance.Warning($"load startup : {string.Join(",", types.Select(c => c.Name))}");
        }

        public static void Add(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            foreach (var startup in startups)
            {
                if (config.Data.Common.Modes.Contains("client"))
                    startup.AddClient(serviceCollection, config, assemblies);
                if (config.Data.Common.Modes.Contains("server"))
                    startup.AddServer(serviceCollection, config, assemblies);
            }
        }
        public static void Use(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            foreach (var startup in startups)
            {
                if (config.Data.Common.Modes.Contains("client"))
                    startup.UseClient(serviceProvider, config, assemblies);
                if (config.Data.Common.Modes.Contains("server"))
                    startup.UseServer(serviceProvider, config, assemblies);
            }
        }
    }
}
