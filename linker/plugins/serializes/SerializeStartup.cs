using linker.config;
using linker.startup;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.serializes
{
    /// <summary>
    /// MemoryPack 序列化扩展加载插件
    /// </summary>
    public sealed class SerializeStartup : IStartup
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
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            MemoryPackFormatterProvider.Register(new IPEndPointFormatter());
            MemoryPackFormatterProvider.Register(new IPAddressFormatter());
        }


        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
        }
    }
}
