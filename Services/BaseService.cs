using System.Net;
using System.Reflection;
using org.wakanda.framework.Attributes;
using org.wakanda.framework.Entity;
using org.wakanda.framework.Enums;
using org.wakanda.framework.Exceptions;
using org.wakanda.framework.MessageBroker;
using org.wakanda.framework.Repositories.Interface;
using org.wakanda.framework.Services.Interface;
using org.wakanda.framework.Utilities;
using Microsoft.Extensions.Localization;
using org.wakanda.framework.Cache.Interface;
using Action = org.wakanda.framework.Enums.Action;
using org.wakanda.framework.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading;
using System.Linq;
using System.IO;
using org.wakanda.framework.Cache;
using org.wakanda.framework.MessageBroker.Interface;


namespace org.wakanda.framework.Services
{
    /// <summary>
    /// Base service for entity operations
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TFilter">The filter type</typeparam>
    /// <typeparam name="TContextMaster">The master database context</typeparam>
    /// <typeparam name="TContextSlave">The slave database context</typeparam>
    public abstract class BaseService<T, TFilter,TContextMaster,TContextSlave> : IBaseService<T, TFilter>
    where T : BaseEntity
    where TFilter : BaseFilter
    where TContextMaster : DbContext
    where TContextSlave : DbContext
    {
        protected readonly IBaseRepository<T, TFilter,TContextMaster,TContextSlave> _repository;
        protected readonly IStringLocalizer<LanguageResponse> _localizer;
        protected readonly IAuditTrailService _auditService;

        private readonly ICacheService _cacheService;
        private readonly CacheSettings _cacheSettings;
        private readonly BaseEntityCacheService<T> _entityCacheService;
        protected readonly TContextMaster _masterContext;
        protected readonly TContextSlave _slaveContext;
        private readonly FileLoggingSettings _logSettings;
        private readonly IBrokerService _brokerService;
        private readonly MessageBrokerSettings _brokerSettings;

        private Action _currentAction = Action.None;

        /// <summary>
        /// Constructor for the base service
        /// </summary>
        public BaseService(
            IBaseRepository<T,TFilter,TContextMaster,TContextSlave> repository,
            IStringLocalizer<LanguageResponse> localizer,
            IAuditTrailService auditService,
            TContextMaster masterContext,
            TContextSlave slaveContext,
            IOptions<FileLoggingSettings> logSettings,
            ICacheService cacheService,
            IOptions<CacheSettings> cacheOptions,
            BaseEntityCacheService<T> entityCacheService,
            IBrokerService brokerService,
            IOptions<MessageBrokerSettings> brokerOptions
            )
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _masterContext = masterContext ?? throw new ArgumentNullException(nameof(masterContext));
            _slaveContext = slaveContext ?? throw new ArgumentNullException(nameof(slaveContext));
            _logSettings = logSettings?.Value ?? new FileLoggingSettings();
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _cacheSettings = cacheOptions?.Value ?? throw new ArgumentNullException(nameof(cacheOptions));
            _entityCacheService = entityCacheService ?? throw new ArgumentNullException(nameof(entityCacheService));
            _brokerService = brokerService ?? throw new ArgumentNullException(nameof(brokerService));
            _brokerSettings = brokerOptions?.Value ?? throw new ArgumentNullException(nameof(brokerOptions));
        }

        /// <summary>
        /// Determines if caching is enabled for this entity type
        /// </summary>
        private bool IsCachingEnabled =>
            _cacheSettings.Enabled
            && typeof(T)
               .GetCustomAttribute<CacheableAttribute>()?
               .EnableCaching == true;

        /// <summary>
        /// Gets the default cache expiry time
        /// </summary>
        private TimeSpan DefaultExpiry =>
            TimeSpan.FromMinutes(_cacheSettings.DefaultExpiryMinutes);

        /// <summary>
        /// Attempts to publish a message to the message broker, with graceful failure
        /// </summary>
        private async Task TryPublishAsync(string topic, MessageBrokerMessage<T> msg, CancellationToken cancellationToken = default)
        {
            if (!_brokerSettings.Enabled) return;
            try
            {
                await _brokerService.PublishAsync(topic, msg, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log the exception but don't fail the operation
                // Consider using a proper logging framework instead of ignoring
                System.Diagnostics.Debug.WriteLine($"Failed to publish message: {ex.Message}");
            }
        }

        /// <summary>
        /// Publishes an event to the message broker based on entity attributes and action
        /// </summary>
        private async Task PublishEventAsync(string eventType, T entity, Action action, CancellationToken cancellationToken = default)
        {
            // Check if events are enabled
            if (!_brokerSettings.Enabled) 
                return;

            // Check if entity has a PublishAttribute
            var attr = typeof(T).GetCustomAttribute<PublishAttribute>(inherit: false);
            if (attr == null) 
                return;

            // Check if entity attribute includes action
            if (!attr.PublishOn.HasFlag(action)) 
                return;

            // Build and send message
            var msg = new MessageBrokerMessage<T>
            {
                MessageType = eventType,
                Payload = entity,
                CorrelationId = Guid.NewGuid().ToString()
            };

            try
            {
                await _brokerService.PublishAsync(typeof(T).Name, msg, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log the exception but don't fail the operation
                System.Diagnostics.Debug.WriteLine($"Failed to publish event: {ex.Message}");
            }
        }
       
        /// <summary>
        /// Pre-save hook for entity validation and processing
        /// </summary>
        protected virtual Task<T> PreSaveAsync(T entity, CancellationToken cancellationToken = default) =>
            entity == null 
                ? throw new PreProcessingException(
                    ErrorType.EMPTY_ENTITY,
                    _localizer["Validation.EmptyEntity"])
                : Task.FromResult(entity);

        /// <summary>
        /// Post-save hook for entity processing
        /// </summary>
        protected virtual Task<T> PostSaveAsync(T oldEntity, T newEntity, CancellationToken cancellationToken = default) => 
            Task.FromResult(newEntity);

        /// <summary>
        /// Saves an entity to the database
        /// </summary>
        public virtual async Task<T> SaveAsync(T entity, CancellationToken cancellationToken = default)
        {
            var preProcessed = await PreSaveAsync(entity, cancellationToken);
            await _repository.AddAsync(preProcessed, cancellationToken);
            var result = await PostSaveAsync(entity, preProcessed, cancellationToken);
            
            if (IsCachingEnabled && result != null)
            {
                try
                {
                    await _entityCacheService.SetEntityAsync(result, DefaultExpiry, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log cache error but continue
                    System.Diagnostics.Debug.WriteLine($"Failed to cache entity: {ex.Message}");
                }
            }
            
            await PublishEventAsync($"{typeof(T).Name}Created", result, Action.Create, cancellationToken);
            return result;
        }

        /// <summary>
        /// Gets all entities
        /// </summary>
        public virtual Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default) => 
            _repository.GetAllAsync(cancellationToken);

        /// <summary>
        /// Gets an entity by ID
        /// </summary>
        public virtual async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            if (IsCachingEnabled)
            {
                try
                {
                    var cached = await _entityCacheService.GetEntityAsync(id, cancellationToken);
                    if (cached != null) return cached;
                }
                catch (Exception ex)
                {
                    // Log cache error but continue with database fetch
                    System.Diagnostics.Debug.WriteLine($"Failed to get cached entity: {ex.Message}");
                }
            }

            var entity = await _repository.GetByIdAsync(id, cancellationToken);

            if (entity == null)
                throw new BaseException(ErrorType.NO_DATA_FOUND, _localizer["Error.NoDataFound"], HttpStatusCode.NotFound);
            
            if (IsCachingEnabled)
            {
                try
                {
                    await _entityCacheService.SetEntityAsync(entity, DefaultExpiry, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log cache error but continue
                    System.Diagnostics.Debug.WriteLine($"Failed to cache entity: {ex.Message}");
                }
            }

            return entity;
        }

        /// <summary>
        /// Updates entity properties from updated to existing entity
        /// </summary>
        protected virtual void UpdateEntityProperties(T existing, T updated)
        {
            // Get all properties except audit fields, key, and excluded properties
            var properties = typeof(T).GetProperties()
                .Where(p => !IsAuditProperty(p.Name) 
                        && !IsKeyProperty(p.Name)
                        && !IsExcludedProperty(p.Name));
            
            foreach (var prop in properties)
            {
                var newValue = prop.GetValue(updated);
                prop.SetValue(existing, newValue);
            }
        }

        /// <summary>
        /// Validates an entity before updating
        /// </summary>
        protected virtual Task ValidateForUpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            // Add any business validation logic here
            return Task.CompletedTask;
        }

        /// <summary>
        /// Determines if a property is an audit property
        /// </summary>
        private bool IsAuditProperty(string name) => 
            name is nameof(BaseEntity.DateCreated) 
            or nameof(BaseEntity.CreatedBy) 
            or nameof(BaseEntity.CreatedByIp)
            or nameof(BaseEntity.DateModified)
            or nameof(BaseEntity.ModifiedBy)
            or nameof(BaseEntity.ModifiedByIp);

        /// <summary>
        /// Determines if a property is a key property
        /// </summary>
        private bool IsKeyProperty(string name) => 
            name.Equals("id", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines if a property should be excluded from updates
        /// </summary>
        private bool IsExcludedProperty(string name) =>
            name is nameof(BaseEntity.IsDeleted)
            or nameof(BaseEntity.Version)
            or nameof(BaseEntity.Status);

        /// <summary>
        /// Updates an entity
        /// </summary>
        public virtual async Task<T> UpdateAsync(T entity, int entityId, string modifiedBy, string modifiedByIp, CancellationToken cancellationToken = default)
        {
            // Validate input
            if (entity == null)
                throw new PreProcessingException(ErrorType.EMPTY_ENTITY, _localizer["Validation.EmptyEntity"]);

            if (entity.Id != entityId)
                throw new PreProcessingException(ErrorType.MISMATCH_IDENTIFIER, _localizer["Validation.MismatchIdentifier"]);

            // Get existing entity with tracking
            var existing = await _repository.GetByIdAsync(entityId, cancellationToken) 
                ?? throw new BaseException(ErrorType.NO_DATA_FOUND, _localizer["Error.NoDataFound"]);

            if (!existing.Equals(entity))
            {
                throw new PreProcessingException(ErrorType.MISMATCH_ENTITY, _localizer["Validation.MismatchEntity"]);
            }

            // Update properties (excluding audit fields)
            UpdateEntityProperties(existing, entity);

            existing.ModifiedBy = modifiedBy;
            existing.ModifiedByIp = modifiedByIp;
        
            // Validate business rules
            await ValidateForUpdateAsync(existing, cancellationToken);

            // Save changes
            await _repository.UpdateAsync(existing, cancellationToken);

            // Invalidate cache if needed
            if (IsCachingEnabled)
            {
                try
                {
                    await _entityCacheService.InvalidateEntityAsync(entityId, cancellationToken);
                    await _entityCacheService.SetEntityAsync(existing, DefaultExpiry, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log cache error but continue
                    System.Diagnostics.Debug.WriteLine($"Failed to update entity cache: {ex.Message}");
                }
            }

            // Publish event
            await PublishEventAsync($"{typeof(T).Name}Updated", existing, Action.Update, cancellationToken);

            return existing;
        }

        /// <summary>
        /// Pre-deletion hook for entity validation and processing
        /// </summary>
        protected virtual async Task<T> PreDeleteAsync(int entityId, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(entityId, cancellationToken)
                ?? throw new BaseException(ErrorType.NO_DATA_FOUND, _localizer["Error.NoDataFound"], HttpStatusCode.NotFound);
            
            return entity;
        }

        /// <summary>
        /// Post-deletion hook for entity processing
        /// </summary>
        protected virtual Task<T> PostDeleteAsync(T entity, CancellationToken cancellationToken = default) => 
            Task.FromResult(entity);

        /// <summary>
        /// Permanently deletes an entity by ID
        /// </summary>
        public virtual async Task<T> DeleteAsync(int entityId, string modifiedBy, string modifiedByIp, CancellationToken cancellationToken = default)
        {
            var entity = await PreDeleteAsync(entityId, cancellationToken);
            
            await _repository.DeleteAsync(entityId, cancellationToken);
            
            // Invalidate cache if needed
            if (IsCachingEnabled)
            {
                try
                {
                    await _entityCacheService.InvalidateEntityAsync(entityId, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log the exception but continue
                    System.Diagnostics.Debug.WriteLine($"Failed to invalidate entity cache: {ex.Message}");
                }
            }
            
            // Publish event
            await PublishEventAsync($"{typeof(T).Name}Deleted", entity, Action.Delete, cancellationToken);
            
            return await PostDeleteAsync(entity, cancellationToken);
        }

        /// <summary>
        /// Deletes an entity by ID
        /// </summary>
        public virtual Task DeleteByIdAsync(int entityId, CancellationToken cancellationToken = default) => 
            _repository.DeleteAsync(entityId, cancellationToken);

        /// <summary>
        /// Pre-soft-delete hook for entity validation and processing
        /// </summary>
        protected virtual async Task PreSoftDeleteAsync(int id, string modifiedBy, string modifiedByIp, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new BaseException(ErrorType.NO_DATA_FOUND, _localizer["Error.NoDataFound"], HttpStatusCode.NotFound);
                
            entity.IsDeleted = true;
            entity.ModifiedBy = modifiedBy;
            entity.ModifiedByIp = modifiedByIp;
            
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        /// <summary>
        /// Post-soft-delete hook for entity processing
        /// </summary>
        protected virtual Task PostSoftDeleteAsync(int id, string modifiedBy, string modifiedByIp, CancellationToken cancellationToken = default) => 
            Task.CompletedTask;

        /// <summary>
        /// Soft deletes an entity by ID (marks as deleted)
        /// </summary>
        public virtual async Task SoftDeleteAsync(int id, string modifiedBy, string modifiedByIp, CancellationToken cancellationToken = default)
        {
            await PreSoftDeleteAsync(id, modifiedBy, modifiedByIp, cancellationToken);
            
            // Invalidate cache if needed
            if (IsCachingEnabled)
            {
                try
                {
                    await _entityCacheService.InvalidateEntityAsync(id, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log cache error but continue
                    System.Diagnostics.Debug.WriteLine($"Failed to invalidate entity cache: {ex.Message}");
                }
            }
            
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity != null)
            {
                await PublishEventAsync($"{typeof(T).Name}SoftDeleted", entity, Action.SoftDelete, cancellationToken);
            }
            
            await PostSoftDeleteAsync(id, modifiedBy, modifiedByIp, cancellationToken);
        }

        /// <summary>
        /// Enables an entity by ID
        /// </summary>
        public virtual async Task EnableAsync(int id, string modifiedBy, string modifiedByIp, CancellationToken cancellationToken = default)
        {
            await PreEnableAsync(id, cancellationToken);
            
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new BaseException(ErrorType.NO_DATA_FOUND, _localizer["Error.NoDataFound"], HttpStatusCode.NotFound);
                
            entity.Status = (int)Status.Active;
            entity.ModifiedBy = modifiedBy;
            entity.ModifiedByIp = modifiedByIp;
            
            await _repository.UpdateAsync(entity, cancellationToken);
            
            // Invalidate cache if needed
            if (IsCachingEnabled)
            {
                try
                {
                    await _entityCacheService.InvalidateEntityAsync(id, cancellationToken);
                    await _entityCacheService.SetEntityAsync(entity, DefaultExpiry, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log cache error but continue
                    System.Diagnostics.Debug.WriteLine($"Failed to update entity cache: {ex.Message}");
                }
            }
            
            await PublishEventAsync($"{typeof(T).Name}Enabled", entity, Action.Enable, cancellationToken);
            await PostEnableAsync(id, cancellationToken);
        }

        /// <summary>
        /// Disables an entity by ID
        /// </summary>
        public virtual async Task DisableAsync(int id, string modifiedBy, string modifiedByIp, CancellationToken cancellationToken = default)
        {
            await PreDisableAsync(id, cancellationToken);
            
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new BaseException(ErrorType.NO_DATA_FOUND, _localizer["Error.NoDataFound"], HttpStatusCode.NotFound);
                
            entity.Status = (int)Status.Inactive;
            entity.ModifiedBy = modifiedBy;
            entity.ModifiedByIp = modifiedByIp;
            
            await _repository.UpdateAsync(entity, cancellationToken);
            
            // Invalidate cache if needed
            if (IsCachingEnabled)
            {
                try
                {
                    await _entityCacheService.InvalidateEntityAsync(id, cancellationToken);
                    await _entityCacheService.SetEntityAsync(entity, DefaultExpiry, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log cache error but continue
                    System.Diagnostics.Debug.WriteLine($"Failed to update entity cache: {ex.Message}");
                }
            }
            
            await PublishEventAsync($"{typeof(T).Name}Disabled", entity, Action.Disable, cancellationToken);
            await PostDisableAsync(id, cancellationToken);
        }

        /// <summary>
        /// Pre-enable hook for entity validation and processing
        /// </summary>
        protected virtual Task PreEnableAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        
        /// <summary>
        /// Post-enable hook for entity processing
        /// </summary>
        protected virtual Task PostEnableAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        
        /// <summary>
        /// Pre-disable hook for entity validation and processing
        /// </summary>
        protected virtual Task PreDisableAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        
        /// <summary>
        /// Post-disable hook for entity processing
        /// </summary>
        protected virtual Task PostDisableAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <summary>
        /// Gets a paged result of entities based on filter criteria
        /// </summary>
        public virtual async Task<PagedResult<T>> GetPagedAsync(TFilter filter, CancellationToken cancellationToken = default)
        {
            ValidateFilter(filter);
            return await _repository.GetPagedAsync(filter, cancellationToken);
        }

        /// <summary>
        /// Validates the filter parameters
        /// </summary>
        private void ValidateFilter(TFilter filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
                
            filter.SearchProperties ??= GetDefaultSearchProperties();
        }

        /// <summary>
        /// Gets the default search properties for filtering
        /// </summary>
        protected virtual List<string> GetDefaultSearchProperties()
        {
            return new List<string>();
        }
    }
}