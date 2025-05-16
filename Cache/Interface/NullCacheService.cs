using System;
using System.Threading.Tasks;
using org.wakanda.framework.Cache.Interface;

namespace org.wakanda.framework.Cache
{
    public class NullCacheService : ICacheService
    {
        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) => Task.CompletedTask;
        public Task<T?> GetAsync<T>(string key) => Task.FromResult(default(T));
        public Task RemoveAsync(string key) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string key) => Task.FromResult(false);
    }
}