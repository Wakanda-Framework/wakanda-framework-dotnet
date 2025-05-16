using System;
using System.Threading.Tasks;
using org.wakanda.framework.Cache.Interface;
using org.wakanda.framework.Entity;

namespace org.wakanda.framework.Cache
{
    public abstract class BaseEntityCacheService<T> where T : BaseEntity
    {
        protected readonly ICacheService _cacheService;
        protected abstract string CacheKeyPrefix { get; }

        protected BaseEntityCacheService(ICacheService cacheService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public async Task SetEntityAsync(T entity, TimeSpan? expiry = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var key = $"{CacheKeyPrefix}:{entity.Id}";
            await _cacheService.SetAsync(key, entity, expiry);
        }

        public async Task<T?> GetEntityAsync(int id)
        {
            var key = $"{CacheKeyPrefix}:{id}";
            return await _cacheService.GetAsync<T>(key);
        }

        public async Task RemoveEntityAsync(int id)
        {
            var key = $"{CacheKeyPrefix}:{id}";
            await _cacheService.RemoveAsync(key);
        }

        public async Task RefreshEntityAsync(T entity, TimeSpan? expiry = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            await SetEntityAsync(entity, expiry);
        }
    }
}
