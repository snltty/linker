using MemoryPack;
using Microsoft.Extensions.Caching.Memory;
namespace linker.plugins.relay.server.caching
{
    public sealed class RelayCachingMemory : IRelayCaching
    {
        public string Name => "memory";

        private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions { });
        public RelayCachingMemory()
        {
        }

        public bool TryAdd<T>(string key, T value, int expired)
        {
            cache.Set(key, MemoryPackSerializer.Serialize(value), TimeSpan.FromMilliseconds(expired));

            return true;
        }
        public bool TryGetValue<T>(string key, out T value)
        {
            bool result = cache.TryGetValue(key, out byte[] bytes);

            if (result)
                value = MemoryPackSerializer.Deserialize<T>(bytes);
            else value = default;

            return true;
        }
    }


}
