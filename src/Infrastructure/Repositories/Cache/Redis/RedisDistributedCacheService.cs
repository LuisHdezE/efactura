using ApplicationCore.Interfaces.Repositories.Cache;
using Infrastructure.RedisDistributedCache;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Repositories.Cache.Redis
{
    public class RedisDistributedCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public RedisDistributedCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task SetRecordAsync<T>(string recordId, T data,
              TimeSpan? absoluteExpireTime = null, TimeSpan? unusedExpireTime = null)
        {
            await _cache.SetRecordAsync(recordId, data, absoluteExpireTime, unusedExpireTime);
        }

        public async Task<T> GetRecordAsync<T>(string recordId)
        {
            return await _cache.GetRecordAsync<T>(recordId);
        }

        public async Task<T> RefreshRecordAsync<T>(string recordId, T data,
            TimeSpan? absoluteExpireTime = null, TimeSpan? unusedExpireTime = null)
        {
            return await _cache.RefreshRecordAsync(recordId, data, absoluteExpireTime, unusedExpireTime);
        }
    }
}