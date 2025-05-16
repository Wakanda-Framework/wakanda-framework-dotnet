using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using org.wakanda.framework.Attributes;
using org.wakanda.framework.Entity;
using org.wakanda.framework.Repositories.Interface;
using org.wakanda.framework.Settings;
using org.wakanda.framework.Services.Interface;
using org.wakanda.framework.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace org.wakanda.framework.Repositories
{
    public abstract class BaseRepository<T, TFilter, TContextMaster,TContextSlave>
        : IBaseRepository<T, TFilter, TContextMaster,TContextSlave>
        where T : BaseEntity
        where TFilter : BaseFilter
        where TContextMaster : DbContext
        where TContextSlave : DbContext
    {
        protected readonly TContextMaster masterContext;
        protected readonly TContextSlave slaveContext;
        protected readonly IAuditTrailService _auditService;
        private readonly FileLoggingSettings _logSettings;
        protected abstract List<string> DefaultSearchProperties { get; }
        protected abstract HashSet<string> SortableColumns { get; }

        public BaseRepository(
            TContextMaster masterContext,
            TContextSlave slaveContext,
            IAuditTrailService auditService,
            IOptions<FileLoggingSettings> logSettings
            )
        {
            this.masterContext = masterContext ?? throw new ArgumentNullException(nameof(masterContext));
            this.slaveContext = slaveContext ?? throw new ArgumentNullException(nameof(slaveContext));
            _auditService      = auditService;
            _logSettings       = logSettings.Value;
        }

        protected DbContext GetContext(bool isWrite = false)
        {
            return isWrite ? masterContext : slaveContext;
        }

        public Task<T?> GetByIdAsync(int id) =>
            GetContext().Set<T>().FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        public Task<IEnumerable<T>> GetAllAsync() =>
            GetContext().Set<T>().Where(e => !e.IsDeleted).ToListAsync()
                   .ContinueWith(t => (IEnumerable<T>)t.Result);

        public async Task AddAsync(T entity)
        {
            GetContext(true).Set<T>().Add(entity);
            await SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
            GetContext(true).Set<T>().Update(entity);
            await SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null) return;

            var master = GetContext(true);

            master.Entry(entity).State = EntityState.Deleted;
            master.Set<T>().Remove(entity);
            await SaveChangesAsync();
        }

        public async Task MarkAsDeletedAsync(int id, string modifiedBy, string modifiedByIp)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null) return;

            entity.MarkAsDeleted(modifiedBy, modifiedByIp);
            GetContext(true).Set<T>().Update(entity);
            await SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(int id, string status, string modifiedBy, string modifiedByIp)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null) return;

            entity.Status       = status;
            entity.DateModified = UnixTimeHelper.CurrentUnixTimestamp();
            GetContext(true).Set<T>().Update(entity);
            await SaveChangesAsync();
        }

        public async Task<PagedResult<T>> GetPagedAsync(TFilter filter)
        {
            var query = GetContext().Set<T>().Where(e => !e.IsDeleted);
            query = ApplySearch(query, filter);
            query = ApplyCustomFilters(query, filter);

            var total = await query.CountAsync();
            query = ApplySorting(query, filter)
                    .Skip((filter.PageNo - 1) * filter.PageSize)
                    .Take(filter.PageSize);

            return new PagedResult<T>
            {
                Items      = await query.ToListAsync(),
                TotalCount = total,
                PageNo     = filter.PageNo,
                PageSize   = filter.PageSize
            };
        }

        protected virtual IQueryable<T> ApplySearch(IQueryable<T> query, TFilter filter)
        {
            if (string.IsNullOrEmpty(filter.SearchTerm)) 
                return query;

            var props = filter.SearchProperties?.Any() == true
                        ? filter.SearchProperties
                        : DefaultSearchProperties;
            return query.Where(BuildSearchExpression(props, filter.SearchTerm));
        }

        protected abstract IQueryable<T> ApplyCustomFilters(IQueryable<T> query, TFilter filter);

        private IQueryable<T> ApplySorting(IQueryable<T> query, TFilter filter)
        {
            var col = SortableColumns.Contains(filter.SortBy) 
                      ? filter.SortBy 
                      : "Id";

            return filter.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(e => EF.Property<object>(e, col))
                : query.OrderBy(e => EF.Property<object>(e, col));
        }

        private static Expression<Func<T, bool>> BuildSearchExpression(List<string> props, string term)
        {
            var param = Expression.Parameter(typeof(T), "e");
            Expression? combined = null;

            foreach (var name in props)
            {
                var pi = typeof(T).GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (pi == null) continue;

                Expression? check = null;
                if (pi.PropertyType == typeof(string))
                {
                    var propExpr = Expression.Property(param, pi);
                    var notNull  = Expression.NotEqual(propExpr, Expression.Constant(null, typeof(string)));
                    var toLower  = Expression.Call(propExpr, nameof(string.ToLower), Type.EmptyTypes);
                    var contains = Expression.Call(toLower, nameof(string.Contains), Type.EmptyTypes, 
                                       Expression.Constant(term.ToLower()));
                    check = Expression.AndAlso(notNull, contains);
                }
                else
                {
                    var enumType = pi.PropertyType.IsEnum
                        ? pi.PropertyType
                        : Nullable.GetUnderlyingType(pi.PropertyType);
                    if (enumType?.IsEnum == true)
                    {
                        try
                        {
                            var parsed = Enum.Parse(enumType, term, true);
                            Expression access = Expression.Property(param, pi);
                            if (pi.PropertyType != enumType)
                                access = Expression.Convert(access, enumType);
                            check = Expression.Equal(access, Expression.Constant(parsed, enumType));
                        }
                        catch { /* ignore invalid parse */ }
                    }
                }

                if (check != null)
                    combined = combined == null ? check : Expression.OrElse(combined, check);
            }

            var body = combined ?? Expression.Constant(true);
            return Expression.Lambda<Func<T, bool>>(body, param);
        }

        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Collect audit trails before saving to avoid tracking issues
            var auditRecords = CollectAuditTrails();
            int savedCount = 0;

            // Use execution strategy for retries
            var strategy = masterContext.Database.CreateExecutionStrategy();
            
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await masterContext.Database.BeginTransactionAsync(cancellationToken);
                
                try
                {
                    // Save main entities first
                    savedCount = await masterContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    
                    //_logger.LogInformation("Successfully saved {Count} entities", savedCount);
                }
                catch (Exception)
                {
                    //_logger.LogError(ex, "Error saving entities");
                    await transaction.RollbackAsync(cancellationToken);
                    throw; // Re-throw after rollback
                }
            });

            // Process audit trails separately
            if (auditRecords.Any())
            {
                await ProcessAuditTrailsAsync(auditRecords, cancellationToken);
            }

            return savedCount;
        }

        private List<AuditTrail> CollectAuditTrails()
        {
            var entries = masterContext.ChangeTracker
                .Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Where(e => e.Entity.GetType().IsDefined(typeof(AuditableAttribute), false))
                .ToList();

            return entries.Select(e => 
            {
                var entity = (BaseEntity)e.Entity;
                return new AuditTrail
                {
                    EntityType = e.Entity.GetType().Name,
                    EntityId = entity.Id,
                    Timestamp = DateTime.UtcNow,
                    Action = e.State.ToString(),
                    UserId = e.State == EntityState.Added ? entity.CreatedBy : entity.ModifiedBy,
                    UserIp = e.State == EntityState.Added ? entity.CreatedByIp : entity.ModifiedByIp,
                    OldValues = e.State switch {
                        EntityState.Modified => JsonSerializer.Serialize(e.OriginalValues.ToObject()),
                        EntityState.Deleted => JsonSerializer.Serialize(e.Entity),
                        _ => null
                    },
                    NewValues = e.State switch {
                        EntityState.Added => JsonSerializer.Serialize(e.Entity),
                        EntityState.Modified => JsonSerializer.Serialize(e.CurrentValues.ToObject()),
                        _ => null
                    }
                };
            }).ToList();
        }

        private async Task ProcessAuditTrailsAsync(
            List<AuditTrail> auditRecords,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1) Persist via the audit‐trail service:
                //    assume your IAuditTrailService has an overload that accepts many records
                await _auditService.LogAuditAsync(auditRecords);

                //_logger.LogInformation("Persisted {Count} audit records via IAuditTrailService", auditRecords.Count);

                // 2) Optional file logging
                if (_logSettings.EnableFileLogging)
                {
                    await LogToFileAsync(auditRecords, cancellationToken);
                   // _logger.LogInformation("Appended {Count} audit records to file", auditRecords.Count);
                }
            }
            catch (Exception)
            {
                // We swallow errors here so they don't break the main SaveChanges
               // _logger.LogError(ex, "Error processing audit trails");
            }
        }
      
              private async Task LogToFileAsync(List<AuditTrail> auditRecords, CancellationToken cancellationToken)
        {
            try
            {
                var path = _logSettings.LogFilePath ?? "basequc-audit.log";
                var directory = Path.GetDirectoryName(path);
                
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var lines = auditRecords.Select(r => 
                    $"[{r.Timestamp:yyyy-MM-dd HH:mm:ss}] {JsonSerializer.Serialize(r)}");
                
                await File.AppendAllLinesAsync(path, lines, cancellationToken);
            }
            catch (Exception)
            {
                //_logger.LogError(ex, "Error writing audit log to file");
            }
        }



    }
}
