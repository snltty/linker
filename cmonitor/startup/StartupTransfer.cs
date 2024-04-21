using cmonitor.config;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.startup
{
    public static class StartupTransfer
    {
        static List<IStartup> startups;
        public static void Init(Config config)
        {
            var types = ReflectionHelper.GetInterfaceSchieves(typeof(IStartup));
            if (config.Data.Common.PluginNames.Length > 0)
            {
                types = types.Where(c => config.Data.Common.PluginNames.Any(d => c.FullName.Contains(d)));
            }
            startups = types.Select(c => Activator.CreateInstance(c) as IStartup).ToList();

            Logger.Instance.Warning($"load startup : {string.Join(",", types.Select(c=>c.Name))}");
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
