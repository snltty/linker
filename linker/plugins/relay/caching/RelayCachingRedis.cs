using linker.config;
using linker.libs.jsonConverters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Unicode;
namespace linker.plugins.relay.caching
{
    public sealed class RelayCachingRedis : IRelayCaching
    {
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true,
            Converters = { new IPAddressJsonConverter(), new IPEndpointJsonConverter(), new DateTimeConverter() }
        };

        private readonly IDistributedCache cache;
        public RelayCachingRedis(FileConfig fileConfig)
        {
            cache = new RedisCache(new RedisCacheOptions
            {
                Configuration = fileConfig.Data.Server.Relay.Caching.ConnectString
            });
        }

        public async ValueTask<bool> TryAdd<T>(string key, T value, int expired)
        {
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            await cache.SetStringAsync(key, JsonSerializer.Serialize(value, jsonSerializerOptions), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(expired) });
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            return true;
        }
        public async ValueTask<bool> TryGetValue<T>(string key, RelayCachingValue<T> wrap)
        {
            string value = await cache.GetStringAsync(key);
            if (string.IsNullOrWhiteSpace(value) == false)
            {
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                wrap.Value = JsonSerializer.Deserialize<T>(value, jsonSerializerOptions);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                return true;
            }
            return false;
        }
    }
}
