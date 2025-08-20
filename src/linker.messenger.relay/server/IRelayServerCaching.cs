using linker.libs;
using Microsoft.Extensions.Caching.Memory;

namespace linker.messenger.relay.server
{
    public interface IRelayServerCaching
    {
        public string Name { get; }

        public bool TryAdd<T>(string key, T value, int expired);
        public bool TryGetValue<T>(string key, out T value);
    }


    public sealed class RelayServerCachingMemory : IRelayServerCaching
    {
        public string Name => "memory";

        private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions { });

        private readonly ISerializer serializer;
        public RelayServerCachingMemory(ISerializer serializer)
        {
            this.serializer = serializer;
        }

        public bool TryAdd<T>(string key, T value, int expired)
        {
            cache.Set(key, serializer.Serialize(value), TimeSpan.FromMilliseconds(expired));

            return true;
        }
        public bool TryGetValue<T>(string key, out T value)
        {
            bool result = cache.TryGetValue(key, out byte[] bytes);

            if (result)
                value = serializer.Deserialize<T>(bytes);
            else value = default;

            return true;
        }
    }

}
