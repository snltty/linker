using cmonitor.config;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.startup
{
    public interface IStartup
    {
        public StartupLevel Level { get; }

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies);
        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies);

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies);
        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies);
    }

    public enum StartupLevel
    {
        Low9 = -9,
        Low8 = -8,
        Low7 = -7,
        Low6 = -6,
        Low5 = -5,
        Low4 = -4,
        Low3 = -3,
        Low2 = -2,
        Low1 = -1,
        Normal = 0,
        Hight1 = 1,
        Hight2 = 2,
        Hight3 = 3,
        Hight4 = 4,
        Hight5 = 5,
        Hight6 = 6,
        Hight7 = 7,
        Hight8 = 8,
        Hight9 = 9,

        Top = int.MaxValue
    }
}
