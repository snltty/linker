using cmonitor.config;
using cmonitor.startup;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.serializes
{
    public sealed class SerializeStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Hight9;
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            MemoryPackFormatterProvider.Register(new IPEndPointFormatter());
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            MemoryPackFormatterProvider.Register(new IPEndPointFormatter());
        }


        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
