using System.Collections.Generic;
using System.Threading.Tasks;
using org.wakanda.framework.Entity;
using org.wakanda.framework.Utilities;
using Microsoft.EntityFrameworkCore;

namespace org.wakanda.framework.Repositories.Interface
{
    public interface IBaseRepository<T, TFilter,TContextMaster,TContextSlave> 
    where T : BaseEntity
    where TFilter : BaseFilter
    where TContextMaster : DbContext
    where TContextSlave : DbContext
    {
        Task<PagedResult<T>> GetPagedAsync(TFilter filter);
        Task<T?> GetByIdAsync(int id); // Explicitly nullable
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task MarkAsDeletedAsync(int id, string modifiedBy, string modifiedByIp);
        Task UpdateStatusAsync(int id, string status, string modifiedBy, string modifiedByIp);

    }
}