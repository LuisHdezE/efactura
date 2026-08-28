namespace ApplicationCore.Interfaces.Repositories.Cache
{
    public interface ICacheService
    {
        Task SetRecordAsync<T>(string recordId, T data, TimeSpan? absoluteExpireTime = null, TimeSpan? unusedExpireTime = null);

        Task<T> GetRecordAsync<T>(string recordId);

        Task<T> RefreshRecordAsync<T>(string recordId, T data, TimeSpan? absoluteExpireTime = null, TimeSpan? unusedExpireTime = null);
    }
}