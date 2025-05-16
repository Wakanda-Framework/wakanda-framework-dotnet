using System;
using System.Threading.Tasks;
using org.wakanda.framework.Entity;


namespace org.wakanda.framework.Cache.Interface
{
    public interface IBaseEntityCacheService<T> where T : BaseEntity
    {
        Task SetEntityAsync(T entity, TimeSpan? expiry = null);
        Task<T?> GetEntityAsync(int id);
        Task RemoveEntityAsync(int id);
        Task RefreshEntityAsync(T? entity, TimeSpan? expiry = null);
    }
}