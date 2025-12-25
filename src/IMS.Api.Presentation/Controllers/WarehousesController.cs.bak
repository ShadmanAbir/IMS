using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using IMS.Api.Application.Commands.Warehouses;
using IMS.Api.Application.Queries.Warehouses;
using IMS.Api.Application.Common.Models;
using IMS.Api.Infrastructure.Data.DTOs;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Controller for managing warehouses
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class WarehousesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WarehousesController> _logger;

    public WarehousesController(IMediator mediator, ILogger<WarehousesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of warehouses
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="includeDeleted">Include soft-deleted warehouses (default: false)</param>
    /// <returns>Paginated list of warehouses</returns>
    [HttpGet]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<PagedResult<WarehouseDto>>> GetWarehouses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool includeDeleted = false)
    {
        var query = new GetWarehousesQuery
        {
            Page = page,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            IsActive = isActive,
            IncludeDeleted = includeDeleted
        };

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new
            {
                ErrorCode = result.ErrorCode,
                Message = result.ErrorMessage,
                Details = new Dictionary<string, object>(),
                TimestampUtc = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets a specific warehouse by ID
    /// </summary>
    /// <param name="id">Warehouse ID</param>
    /// <returns>Warehouse details</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<WarehouseDto>> GetWarehouse(Guid id)
    {
        var query = new GetWarehouseQuery { Id = id };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "WAREHOUSE_NOT_FOUND")
            {
                return NotFound(new
                {
                    ErrorCode = result.ErrorCode,
                    Message = result.ErrorMessage,
                    Details = new Dictionary<string, object>(),
                    TimestampUtc = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new
            {
                ErrorCode = result.ErrorCode,
                Message = result.ErrorMessage,
                Details = new Dictionary<string, object>(),
                TimestampUtc = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Creates a new warehouse
    /// </summary>
    /// <param name="command">Warehouse creation data</param>
    /// <returns>Created warehouse</returns>
    [HttpPost]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<WarehouseDto>> CreateWarehouse([FromBody] CreateWarehouseCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "DUPLICATE_CODE")
            {
                return Conflict(new
                {
                    ErrorCode = result.ErrorCode,
                    Message = result.ErrorMessage,
                    Details = new Dictionary<string, object>(),
                    TimestampUtc = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new
            {
                ErrorCode = result.ErrorCode,
                Message = result.ErrorMessage,
                Details = new Dictionary<string, object>(),
                TimestampUtc = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return CreatedAtAction(
            nameof(GetWarehouse),
            new { id = result.Value.Id },
            result.Value);
    }

    /// <summary>
    /// Updates an existing warehouse
    /// </summary>
    /// <param name="id">Warehouse ID</param>
    /// <param name="command">Warehouse update data</param>
    /// <returns>Updated warehouse</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<WarehouseDto>> UpdateWarehouse(
        Guid id,
        [FromBody] UpdateWarehouseCommand command)
    {
        command.Id = id; // Ensure the ID from the route is used

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "WAREHOUSE_NOT_FOUND")
            {
                return NotFound(new
                {
                    ErrorCode = result.ErrorCode,
                    Message = result.ErrorMessage,
                    Details = new Dictionary<string, object>(),
                    TimestampUtc = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            if (result.ErrorCode == "DUPLICATE_CODE")
            {
                return Conflict(new
                {
                    ErrorCode = result.ErrorCode,
                    Message = result.ErrorMessage,
                    Details = new Dictionary<string, object>(),
                    TimestampUtc = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new
            {
                ErrorCode = result.ErrorCode,
                Message = result.ErrorMessage,
                Details = new Dictionary<string, object>(),
                TimestampUtc = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Soft deletes a warehouse
    /// </summary>
    /// <param name="id">Warehouse ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult> DeleteWarehouse(Guid id)
    {
        // Get the current user ID from claims
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new
            {
                ErrorCode = "INVALID_TOKEN",
                Message = "User ID not found in token",
                Details = new Dictionary<string, object>(),
                TimestampUtc = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var command = new DeleteWarehouseCommand
        {
            Id = id,
            DeletedBy = userId
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "WAREHOUSE_NOT_FOUND")
            {
                return NotFound(new
                {
                    ErrorCode = result.ErrorCode,
                    Message = result.ErrorMessage,
                    Details = new Dictionary<string, object>(),
                    TimestampUtc = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new
            {
                ErrorCode = result.ErrorCode,
                Message = result.ErrorMessage,
                Details = new Dictionary<string, object>(),
                TimestampUtc = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Gets active warehouses only
    /// </summary>
    /// <returns>List of active warehouses</returns>
    [HttpGet("active")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<List<WarehouseDto>>> GetActiveWarehouses()
    {
        var query = new GetWarehousesQuery
        {
            IsActive = true,
            Page = 1,
            PageSize = 100 // Get all active warehouses
        };

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new
            {
                ErrorCode = result.ErrorCode,
                Message = result.ErrorMessage,
                Details = new Dictionary<string, object>(),
                TimestampUtc = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(result.Value.Items);
    }

    /// <summary>
    /// Gets warehouse configuration and capacity information
    /// </summary>
    /// <param name="id">Warehouse ID</param>
    /// <returns>Warehouse configuration details</returns>
    [HttpGet("{id:guid}/configuration")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<object>> GetWarehouseConfiguration(Guid id)
    {
        var query = new GetWarehouseQuery { Id = id };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "WAREHOUSE_NOT_FOUND")
            {
                return NotFound(new
                {
                    ErrorCode = result.ErrorCode,
                    Message = result.ErrorMessage,
                    Details = new Dictionary<string, object>(),
                    TimestampUtc = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new
            {
                ErrorCode = result.ErrorCode,
                Message = result.ErrorMessage,
                Details = new Dictionary<string, object>(),
                TimestampUtc = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Return configuration information
        // In a real implementation, this would include capacity, zones, storage types, etc.
        var configuration = new
        {
            WarehouseId = result.Value.Id,
            Name = result.Value.Name,
            Code = result.Value.Code,
            IsActive = result.Value.IsActive,
            Location = new
            {
                Address = result.Value.Address,
                City = result.Value.City,
                State = result.Value.State,
                Country = result.Value.Country,
                PostalCode = result.Value.PostalCode,
                Coordinates = result.Value.Latitude.HasValue && result.Value.Longitude.HasValue
                    ? new { Latitude = result.Value.Latitude.Value, Longitude = result.Value.Longitude.Value }
                    : null
            },
            Configuration = new
            {
                MaxCapacity = (decimal?)null, // Would be implemented based on business requirements
                CurrentUtilization = (decimal?)null,
                StorageZones = new List<object>(), // Would be implemented based on business requirements
                OperatingHours = new
                {
                    OpenTime = (TimeSpan?)null,
                    CloseTime = (TimeSpan?)null,
                    TimeZone = "UTC"
                }
            }
        };

        return Ok(configuration);
    }
}