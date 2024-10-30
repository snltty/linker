using MemoryPack;
using Microsoft.Extensions.Caching.Memory;
namespace linker.plugins.relay.caching
{
    public sealed class RelayCachingMemory : IRelayCaching
    {
        public string Name => "memory";

        private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions { });
        public RelayCachingMemory()
        {
        }

        public async ValueTask<bool> TryAdd<T>(string key, T value, int expired)
        {
            cache.Set(key, MemoryPackSerializer.Serialize(value), TimeSpan.FromMilliseconds(expired));

            return await ValueTask.FromResult(true);
        }
        public async ValueTask<bool> TryGetValue<T>(string key, RelayCachingValue<T> wrap)
        {
            bool result = cache.TryGetValue(key, out byte[] value);
            wrap.Value = MemoryPackSerializer.Deserialize<T>(value);
            return await ValueTask.FromResult(result);
        }
    }


}
