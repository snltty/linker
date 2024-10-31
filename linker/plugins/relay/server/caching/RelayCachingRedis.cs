using linker.config;
using MemoryPack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
namespace linker.plugins.relay.server.caching
{
    public sealed class RelayCachingRedis : IRelayCaching
    {
        public string Name => "redis";

        private readonly IDistributedCache cache;
        public RelayCachingRedis(FileConfig fileConfig)
        {
            cache = new RedisCache(new RedisCacheOptions
            {
                Configuration = fileConfig.Data.Server.Relay.Distributed.Caching.ConnectString,
                InstanceName = "Linker"
            });
        }

        public async ValueTask<bool> TryAdd<T>(string key, T value, int expired)
        {
            await cache.SetAsync(key, MemoryPackSerializer.Serialize(value), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(expired) });
            return true;
        }
        public async ValueTask<bool> TryGetValue<T>(string key, RelayCachingValue<T> wrap)
        {
            byte[] value = await cache.GetAsync(key);
            if (value != null && value.Length > 0)
            {
                wrap.Value = MemoryPackSerializer.Deserialize<T>(value);
                return true;
            }
            return false;
        }
    }
}
