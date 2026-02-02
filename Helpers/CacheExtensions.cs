using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace SecureApi.Helpers
{
    public static class CacheExtensions
    {
        public static async Task SetRecordAsync<T>(
            this IDistributedCache cache,
            string key,
            T data,
            TimeSpan? absoluteExpireTime = null,
            TimeSpan? slidingExpireTime = null)
        {
            var json = JsonSerializer.Serialize(data);

            var options = new DistributedCacheEntryOptions();

            if (absoluteExpireTime.HasValue)
                options.AbsoluteExpirationRelativeToNow = absoluteExpireTime;

            if (slidingExpireTime.HasValue)
                options.SlidingExpiration = slidingExpireTime;

            await cache.SetStringAsync(key, json, options);
        }

        public static async Task<T?> GetRecordAsync<T>(
            this IDistributedCache cache,
            string key)
        {
            var json = await cache.GetStringAsync(key);
            return json == null ? default : JsonSerializer.Deserialize<T>(json);
        }
    }

}
