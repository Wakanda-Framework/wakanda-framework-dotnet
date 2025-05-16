// IRedisCacheService.cs
using System;
using System.Threading.Tasks;

namespace org.wakanda.framework.Cache.Interface
{
    public interface IRedisCacheService
    {
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task<T?> GetAsync<T>(string key);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
    }
}

