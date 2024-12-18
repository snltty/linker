using linker.config;
using linker.libs;
using linker.startup;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;

namespace linker.serializer
{
    /// <summary>
    /// MemoryPack 序列化扩展加载插件
    /// </summary>
    public sealed class SerializerStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Hight9;
        public string Name => "serialize";
        public bool Required => false;
        public string[] Dependent => Array.Empty<string>();
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            MemoryPackFormatterProvider.Register(new IPEndPointFormatter());
            MemoryPackFormatterProvider.Register(new IPAddressFormatter());
            MemoryPackFormatterProvider.Register(new TunnelConnectionFormatter());
            MemoryPackFormatterProvider.Register(new ConnectionFormatter());

            serviceCollection.AddSingleton<ISerializer, PlusMemoryPackSerializer>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            MemoryPackFormatterProvider.Register(new IPEndPointFormatter());
            MemoryPackFormatterProvider.Register(new IPAddressFormatter());
            MemoryPackFormatterProvider.Register(new TunnelConnectionFormatter());
            MemoryPackFormatterProvider.Register(new ConnectionFormatter());

            serviceCollection.AddSingleton<ISerializer, PlusMemoryPackSerializer>();
        }


        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
        }
    }
}
