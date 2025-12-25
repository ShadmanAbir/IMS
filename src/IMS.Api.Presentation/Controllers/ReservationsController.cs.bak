using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using IMS.Api.Application.Commands.Reservations;
using IMS.Api.Application.Queries.Reservations;
using IMS.Api.Application.Common.Models;
using IMS.Api.Infrastructure.Data.DTOs;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Controller for managing stock reservations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReservationsController> _logger;

    public ReservationsController(IMediator mediator, ILogger<ReservationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of reservations
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="variantId">Optional variant filter</param>
    /// <param name="warehouseId">Optional warehouse filter</param>
    /// <param name="status">Optional status filter (Active, Expired, Used, Cancelled)</param>
    /// <param name="referenceNumber">Optional reference number filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="includeExpired">Include expired reservations (default: true)</param>
    /// <param name="includeDeleted">Include soft-deleted reservations (default: false)</param>
    /// <returns>Paginated list of reservations</returns>
    [HttpGet]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<PagedResult<ReservationDto>>> GetReservations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? variantId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? referenceNumber = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] bool includeExpired = true,
        [FromQuery] bool includeDeleted = false)
    {
        var query = new GetReservationsQuery
        {
            Page = page,
            PageSize = pageSize,
            VariantId = variantId,
            WarehouseId = warehouseId,
            Status = status,
            ReferenceNumber = referenceNumber,
            FromDate = fromDate,
            ToDate = toDate,
            IncludeExpired = includeExpired,
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
    /// Gets a specific reservation by ID
    /// </summary>
    /// <param name="id">Reservation ID</param>
    /// <returns>Reservation details</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<ReservationDto>> GetReservation(Guid id)
    {
        var query = new GetReservationQuery { Id = id };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "RESERVATION_NOT_FOUND")
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
    /// Creates a new reservation
    /// </summary>
    /// <param name="command">Reservation creation data</param>
    /// <returns>Created reservation</returns>
    [HttpPost]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult<ReservationDto>> CreateReservation([FromBody] CreateReservationCommand command)
    {
        // Get the current user ID from claims
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            command.CreatedBy = userId;
        }

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "INSUFFICIENT_STOCK")
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

            if (result.ErrorCode == "INVENTORY_NOT_FOUND")
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
            nameof(GetReservation),
            new { id = result.Value.Id },
            result.Value);
    }

    /// <summary>
    /// Modifies an existing reservation
    /// </summary>
    /// <param name="id">Reservation ID</param>
    /// <param name="command">Reservation modification data</param>
    /// <returns>Updated reservation</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult<ReservationDto>> ModifyReservation(
        Guid id,
        [FromBody] ModifyReservationCommand command)
    {
        command.Id = id; // Ensure the ID from the route is used

        // Get the current user ID from claims
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            command.ModifiedBy = userId;
        }

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "RESERVATION_NOT_FOUND")
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

            if (result.ErrorCode == "RESERVATION_NOT_ACTIVE")
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

            if (result.ErrorCode == "INSUFFICIENT_STOCK")
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
    /// Cancels a reservation
    /// </summary>
    /// <param name="id">Reservation ID</param>
    /// <param name="request">Cancellation request data</param>
    /// <returns>No content on success</returns>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult> CancelReservation(
        Guid id,
        [FromBody] CancelReservationRequest request)
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

        var command = new CancelReservationCommand
        {
            Id = id,
            CancellationReason = request.CancellationReason,
            CancelledBy = userId
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "RESERVATION_NOT_FOUND")
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

            if (result.ErrorCode == "RESERVATION_NOT_ACTIVE")
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
    /// Gets reservations by reference number
    /// </summary>
    /// <param name="referenceNumber">Reference number to search for</param>
    /// <returns>List of reservations with the reference number</returns>
    [HttpGet("by-reference/{referenceNumber}")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<List<ReservationDto>>> GetReservationsByReference(string referenceNumber)
    {
        var query = new GetReservationsQuery
        {
            ReferenceNumber = referenceNumber,
            Page = 1,
            PageSize = 100 // Get all reservations for this reference
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
    /// Gets active reservations for monitoring
    /// </summary>
    /// <param name="warehouseId">Optional warehouse filter</param>
    /// <returns>List of active reservations</returns>
    [HttpGet("active")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<List<ReservationDto>>> GetActiveReservations(
        [FromQuery] Guid? warehouseId = null)
    {
        var query = new GetReservationsQuery
        {
            Status = "Active",
            WarehouseId = warehouseId,
            Page = 1,
            PageSize = 100 // Get all active reservations
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
    /// Gets expired reservations for monitoring
    /// </summary>
    /// <param name="warehouseId">Optional warehouse filter</param>
    /// <returns>List of expired reservations</returns>
    [HttpGet("expired")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<List<ReservationDto>>> GetExpiredReservations(
        [FromQuery] Guid? warehouseId = null)
    {
        var query = new GetReservationsQuery
        {
            Status = "Expired",
            WarehouseId = warehouseId,
            Page = 1,
            PageSize = 100 // Get all expired reservations
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
}

/// <summary>
/// Request model for cancelling a reservation
/// </summary>
public class CancelReservationRequest
{
    public string CancellationReason { get; set; } = string.Empty;
}