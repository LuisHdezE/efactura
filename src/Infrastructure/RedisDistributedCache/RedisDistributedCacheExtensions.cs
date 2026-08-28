using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Infrastructure.RedisDistributedCache
{
    public static class RedisDistributedCacheExtensions
    {
        public static async Task SetRecordAsync<T>(this IDistributedCache cache, string recordId, T data,
              TimeSpan? absoluteExpireTime = null, TimeSpan? unusedExpireTime = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpireTime ?? TimeSpan.FromMinutes(60),
                SlidingExpiration = unusedExpireTime
            };

            var jsonData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
            });
            await cache.SetStringAsync(recordId, jsonData, options);
        }

        public static async Task<T> GetRecordAsync<T>(this IDistributedCache cache, string recordId)
        {
            var jsonData = await cache.GetStringAsync(recordId);

            if (jsonData == null)
            {
                return default;
            }
            else
            {
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
        }

        public static async Task<T> RefreshRecordAsync<T>(this IDistributedCache cache, string recordId, T data,
            TimeSpan? absoluteExpireTime = null, TimeSpan? unusedExpireTime = null)
        {
            var jsonData = await cache.GetStringAsync(recordId);

            if (jsonData != null)
            {
                jsonData = System.Text.Json.JsonSerializer.Serialize(data);
                await cache.RefreshRecordAsync(recordId, jsonData, absoluteExpireTime ?? TimeSpan.FromMinutes(60), unusedExpireTime);
            }

            return JsonConvert.DeserializeObject<T>(jsonData);
        }
    }
}