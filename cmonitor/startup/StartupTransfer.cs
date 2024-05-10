using cmonitor.config;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.startup
{
    public static class StartupTransfer
    {
        static List<IStartup> startups = new List<IStartup>();
        public static void Init(Config config, Assembly[] assemblies)
        {
            var types = ReflectionHelper.GetInterfaceSchieves(assemblies, typeof(IStartup));
            List<IStartup> temps = types.Select(c => Activator.CreateInstance(c) as IStartup).OrderByDescending(c => c.Level).ToList();
            TestDependent(temps);
            LoadPlugins(config, temps);
        }
        private static void TestDependent(List<IStartup> temps)
        {
            IEnumerable<string> names = temps.Select(c => c.Name);
            foreach (IStartup item in temps.Where(c => c.Dependent.Length > 0))
            {
                IEnumerable<string> excepts = item.Dependent.Except(names);
                if (excepts.Any())
                {
                    Logger.Instance.Error($"【{item.Name}】dependent by {string.Join(",", excepts)}，but it not exists");
                }
            }
        }

        private static void LoadPlugins(Config config, List<IStartup> temps)
        {
            //只要哪些
            if (config.Data.Common.IncludePlugins.Length > 0)
            {
                temps = temps.Where(c => c.Required || config.Data.Common.IncludePlugins.Contains(c.Name)).ToList();
            }
            //不要哪些
            else if (config.Data.Common.ExcludePlugins.Length > 0)
            {
                temps = temps.Where(c => c.Required || config.Data.Common.ExcludePlugins.Contains(c.Name) == false).ToList();
            }

            LoadDependents(temps, temps.Select(c => c.Name));
            startups = startups.Distinct().ToList();

            Logger.Instance.Warning($"load startup : {string.Join(",", startups.Select(c => c.Name))}");
        }
        private static void LoadDependents(List<IStartup> all, IEnumerable<string> sependents)
        {
            if (sependents.Any() == false) return;

            IEnumerable<IStartup> temps = all.Where(c => sependents.Contains(c.Name));
            IEnumerable<string> _sependents = temps.SelectMany(c => c.Dependent);

            startups.AddRange(temps);

            LoadDependents(all, _sependents);
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
