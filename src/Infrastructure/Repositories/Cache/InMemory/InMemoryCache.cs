using ApplicationCore.Interfaces.Repositories.Cache;
using System.Runtime.Caching;

namespace Infrastructure.Repositories.Cache.InMemory
{
    public class InMemoryCache : ICacheService
    {
        private ObjectCache _memoryCache = MemoryCache.Default;

        Task<T> ICacheService.GetRecordAsync<T>(string recordId)
        {
            throw new NotImplementedException();
        }

        Task<T> ICacheService.RefreshRecordAsync<T>(string recordId, T data, TimeSpan? absoluteExpireTime, TimeSpan? unusedExpireTime)
        {
            throw new NotImplementedException();
        }

        Task ICacheService.SetRecordAsync<T>(string recordId, T data, TimeSpan? absoluteExpireTime, TimeSpan? unusedExpireTime)
        {
            throw new NotImplementedException();
        }
    }
}