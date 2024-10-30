using Microsoft.Extensions.Caching.Memory;
namespace linker.plugins.relay.caching
{
    public sealed class RelayCachingMemory : IRelayCaching
    {
        private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions { });
        public RelayCachingMemory()
        {
        }

        public async ValueTask<bool> TryAdd<T>(string key, T value, int expired)
        {
            cache.Set(key, value, TimeSpan.FromMilliseconds(expired));

            return await ValueTask.FromResult(true);
        }
        public async ValueTask<bool> TryGetValue<T>(string key, RelayCachingValue<T> wrap)
        {
            bool result = cache.TryGetValue(key, out T value);
            wrap.Value = value;
            return await ValueTask.FromResult(result);
        }
    }


}
