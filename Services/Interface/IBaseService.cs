using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using org.wakanda.framework.Entity;
using org.wakanda.framework.Utilities;

namespace org.wakanda.framework.Services.Interface
{
    /// <summary>
    /// Base service interface for entity operations
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TFilter">The filter type</typeparam>
    public interface IBaseService<T, TFilter>
    where T : BaseEntity
    where TFilter : BaseFilter
    {
        /// <summary>
        /// Saves a new entity
        /// </summary>
        Task<T> SaveAsync(T entity, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all entities
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets an entity by ID
        /// </summary>
        Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an existing entity
        /// </summary>
        Task<T> UpdateAsync(T entity, int entityId, string modifiedBy, string modifiedByIp, CancellationToken cancellationToken = default);
        //Task<T> UpdateEntityAsync(int id, T updatedEntity, string modifiedBy, string modifiedByIp);

        /// <summary>
        /// Permanently deletes an entity by ID
        /// </summary>
        Task<T> DeleteAsync(int entityId, string modifiedBy, string modifiedByIp, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes an entity by ID
        /// </summary>
        Task DeleteByIdAsync(int entityId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Soft deletes an entity by ID (marks as deleted)
        /// </summary>
        Task SoftDeleteAsync(int id, string modifiedBy, string modifiedByIp, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Enables an entity by ID
        /// </summary>
        Task EnableAsync(int id, string modifiedBy, string modifiedByIp, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Disables an entity by ID
        /// </summary>
        Task DisableAsync(int id, string modifiedBy, string modifiedByIp, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paged result of entities based on filter criteria
        /// </summary>
        Task<PagedResult<T>> GetPagedAsync(TFilter filter, CancellationToken cancellationToken = default);
    }
}