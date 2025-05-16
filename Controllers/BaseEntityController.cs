using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using org.wakanda.framework.Entity;
using org.wakanda.framework.Services.Interface;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using org.wakanda.framework.Helpers;
using Microsoft.Extensions.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using org.wakanda.framework.Exceptions;
using org.wakanda.framework.Enums;
using org.wakanda.framework.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;

namespace org.wakanda.framework.Controllers
{
    /// <summary>
    /// Base controller for entity operations
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TFilter">The filter type</typeparam>
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseEntityController<T,TFilter> : ControllerBase 
    where T : BaseEntity
    where TFilter : BaseFilter
    {
        protected readonly IBaseService<T,TFilter> _baseService;
        protected readonly IStringLocalizer<LanguageResponse> _responseLocalizer;
        
        /// <summary>
        /// Constructor for the base entity controller
        /// </summary>
        /// <param name="baseService">The base service for entity operations</param>
        /// <param name="responseLocalizer">The string localizer for responses</param>
        /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
        public BaseEntityController(
            IBaseService<T,TFilter> baseService,
            IStringLocalizer<LanguageResponse> responseLocalizer)
        {
            _baseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
            _responseLocalizer = responseLocalizer ?? throw new ArgumentNullException(nameof(responseLocalizer));
        }

        /// <summary>
        /// Gets all entities with pagination
        /// </summary>
        /// <param name="filter">Filter criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paged list of entities</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public virtual async Task<IActionResult> GetAll([FromQuery] TFilter filter, CancellationToken cancellationToken = default)
        {
            var result = await _baseService.GetPagedAsync(filter, cancellationToken);

            var payload = new
            {
                result.Items,
                Pagination = new
                {
                    result.TotalCount,
                    result.PageNo,
                    result.PageSize,
                    TotalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize)
                }
            };

            var message = GetLocalizedMessage("EntitiesRetrievedSuccessfully");

            return ResponseHelper.Ok(payload, message);
        }

        /// <summary>
        /// Gets an entity by ID
        /// </summary>
        /// <param name="id">The entity ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The entity if found</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _baseService.GetByIdAsync(id, cancellationToken);
                return ResponseHelper.Ok(entity, GetLocalizedMessage("EntityRetrievedSuccessfully"));
            }
            catch (BaseException ex)
            {
                return ResponseHelper.Error<T>(
                    ex.HttpStatusCode,
                    ResponseType.NOT_FOUND,
                    ex.Message);
            }
            catch (Exception)
            {
                return ResponseHelper.Error<T>(
                    HttpStatusCode.NotFound,
                    ResponseType.NOT_FOUND,
                    GetLocalizedMessage("EntityNotFound"));
            }
        }

        /// <summary>
        /// Creates a new entity
        /// </summary>
        /// <param name="entity">The entity to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created entity</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public virtual async Task<IActionResult> Create([FromBody] T entity, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return ResponseHelper.Error<T>(
                    HttpStatusCode.BadRequest,
                    ResponseType.BAD_REQUEST,
                    GetLocalizedMessage("InvalidEntityData"));
            }

            entity.IsDeleted = false;
            entity.CreatedByIp = GetClientIpAddress();
            entity.DateCreated = UnixTimeHelper.CurrentUnixTimestamp();
            var createdEntity = await _baseService.SaveAsync(entity, cancellationToken);

            return ResponseHelper.Created(
                nameof(GetById),
                new { id = createdEntity.Id },
                createdEntity,
                GetLocalizedMessage("EntityCreatedSuccessfully"));
        }

        /// <summary>
        /// Updates an entity by ID
        /// </summary>
        /// <param name="id">The entity ID</param>
        /// <param name="updatedEntity">The updated entity data</param>
        /// <param name="modifiedBy">The user who modified the entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated entity</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public virtual async Task<IActionResult> UpdateEntityById(
            int id,
            [FromBody] T updatedEntity,
            [FromQuery] string modifiedBy,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return ResponseHelper.Error<T>(
                    HttpStatusCode.BadRequest,
                    ResponseType.BAD_REQUEST,
                    GetLocalizedMessage("InvalidEntityData"));
            }
            try
            {
                var updated = await _baseService.UpdateAsync(
                    updatedEntity,
                    id,
                    modifiedBy,
                    GetClientIpAddress(),
                    cancellationToken);

                return ResponseHelper.Ok(updated, _responseLocalizer["EntityUpdatedSuccessfully"]);
            }
            catch (PreProcessingException ex)
            {
                return ResponseHelper.Error<T>(
                    ex.HttpStatusCode,
                    ResponseType.BAD_REQUEST,
                    ex.Message);
            }
            catch (BaseException ex)
            {
                return ResponseHelper.Error<T>(
                    ex.HttpStatusCode,
                    ResponseType.NOT_FOUND,
                    ex.Message);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ResponseHelper.Error<T>(
                    HttpStatusCode.Conflict,
                    ResponseType.SERVER_ERROR,
                   GetLocalizedMessage("ConcurrencyConflict"));
            }
            catch (Exception ex)
            {
                return ResponseHelper.Error<T>(
                    HttpStatusCode.InternalServerError,
                    ResponseType.UNKNOWN_ERROR,
                    GetLocalizedMessage("UpdateFailed") + ex.Message);
            }
        }

        /// <summary>
        /// Soft deletes an entity by ID
        /// </summary>
        /// <param name="id">The entity ID</param>
        /// <param name="modifiedBy">The user who soft deleted the entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> SoftDelete(
            int id,
            [FromQuery] string modifiedBy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _baseService.SoftDeleteAsync(id, modifiedBy, GetClientIpAddress(), cancellationToken);
                return ResponseHelper.DefaultOk(GetLocalizedMessage("EntitySoftDeletedSuccessfully"));
            }
            catch (BaseException ex)
            {
                return ResponseHelper.Error<T>(
                    ex.HttpStatusCode,
                    ResponseType.NOT_FOUND,
                    ex.Message);
            }
            catch (Exception)
            {
                return ResponseHelper.Error<T>(
                    HttpStatusCode.NotFound,
                    ResponseType.NOT_FOUND,
                    GetLocalizedMessage("EntityNotFound"));
            }
        }

        /// <summary>
        /// Enables an entity by ID
        /// </summary>
        /// <param name="id">The entity ID</param>
        /// <param name="modifiedBy">The user who enabled the entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        [HttpPost("enable/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> Enable(
            int id,
            [FromQuery] string modifiedBy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _baseService.EnableAsync(id, modifiedBy, GetClientIpAddress(), cancellationToken);
                return ResponseHelper.DefaultOk(GetLocalizedMessage("EntityEnabledSuccessfully"));
            }
            catch (BaseException ex)
            {
                return ResponseHelper.Error<T>(
                    ex.HttpStatusCode,
                    ResponseType.NOT_FOUND,
                    ex.Message);
            }
            catch (Exception)
            {
                return ResponseHelper.Error<T>(
                    HttpStatusCode.NotFound,
                    ResponseType.NOT_FOUND,
                    GetLocalizedMessage("EntityNotFound"));
            }
        }

        /// <summary>
        /// Disables an entity by ID
        /// </summary>
        /// <param name="id">The entity ID</param>
        /// <param name="modifiedBy">The user who disabled the entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        [HttpPost("disable/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> Disable(
            int id,
            [FromQuery] string modifiedBy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _baseService.DisableAsync(id, modifiedBy, GetClientIpAddress(), cancellationToken);
                return ResponseHelper.DefaultOk(GetLocalizedMessage("EntityDisabledSuccessfully"));
            }
            catch (BaseException ex)
            {
                return ResponseHelper.Error<T>(
                    ex.HttpStatusCode,
                    ResponseType.NOT_FOUND,
                    ex.Message);
            }
            catch (Exception)
            {
                return ResponseHelper.Error<T>(
                    HttpStatusCode.NotFound,
                    ResponseType.NOT_FOUND,
                    GetLocalizedMessage("EntityNotFound"));
            }
        }

        /// <summary>
        /// Hard deletes an entity by ID
        /// </summary>
        /// <param name="id">The entity ID</param>
        /// <param name="modifiedBy">The user who hard deleted the entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message</returns>
        [HttpDelete("HardDelete/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> HardDelete(
            int id,
            [FromQuery] string modifiedBy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _baseService.DeleteAsync(id, modifiedBy, GetClientIpAddress(), cancellationToken);
                return ResponseHelper.DefaultOk(GetLocalizedMessage("EntityDeletedSuccessfully"));
            }
            catch (BaseException ex)
            {
                return ResponseHelper.Error<T>(
                    ex.HttpStatusCode,
                    ResponseType.NOT_FOUND,
                    ex.Message);
            }
            catch (Exception)
            {
                return ResponseHelper.Error<T>(
                    HttpStatusCode.NotFound,
                    ResponseType.NOT_FOUND,
                    GetLocalizedMessage("EntityNotFound"));
            }
        }

        /// <summary>
        /// Gets the client IP address with forwarded header support
        /// </summary>
        /// <returns>The client IP address</returns>
        private string GetClientIpAddress()
        {
            var forwardedHeader = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedHeader))
            {
                // X-Forwarded-For may contain multiple IPs, get the first one
                var ips = forwardedHeader.Split(',', StringSplitOptions.RemoveEmptyEntries);
                return ips[0].Trim();
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        /// <summary>
        /// Gets a localized message from the response localizer
        /// </summary>
        /// <param name="key">The message key</param>
        /// <returns>The localized message</returns>
        protected string GetLocalizedMessage(string key)
        {
            // Get current culture from request
            var culture = HttpContext?.Features?.Get<IRequestCultureFeature>()?.RequestCulture?.Culture 
                ?? CultureInfo.CurrentCulture;
            
            // Get localized string
            var localizedString = _responseLocalizer[key];
            
            // Return value with culture fallback
            return localizedString.ResourceNotFound 
                ? _responseLocalizer[key, CultureInfo.GetCultureInfo("en")] // Fallback to English
                : localizedString.Value;
        }
    }
}