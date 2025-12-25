using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using IMS.Api.Application.Commands.StockMovements;
using IMS.Api.Application.Queries.Refunds;
using IMS.Api.Application.Queries.Inventory;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Controller for managing refunds and return processing
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class RefundsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RefundsController> _logger;

    public RefundsController(IMediator mediator, ILogger<RefundsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Validates a refund request against the original sale
    /// </summary>
    /// <param name="originalSaleReference">Original sale reference number</param>
    /// <param name="variantId">Variant ID</param>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <param name="requestedQuantity">Requested refund quantity</param>
    /// <returns>Refund validation information</returns>
    [HttpGet("validate")]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult<RefundValidationDto>> ValidateRefund(
        [FromQuery] string originalSaleReference,
        [FromQuery] Guid variantId,
        [FromQuery] Guid warehouseId,
        [FromQuery] decimal requestedQuantity)
    {
        var query = new GetRefundValidationQuery
        {
            OriginalSaleReference = originalSaleReference,
            VariantId = variantId,
            WarehouseId = warehouseId,
            RequestedQuantity = requestedQuantity
        };

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "ORIGINAL_SALE_NOT_FOUND")
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
    /// Gets sale information by reference number
    /// </summary>
    /// <param name="referenceNumber">Sale reference number</param>
    /// <returns>Sale information</returns>
    [HttpGet("sales/{referenceNumber}")]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult<SaleInfoDto>> GetSaleInfo(string referenceNumber)
    {
        var query = new GetSaleInfoQuery { ReferenceNumber = referenceNumber };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "SALE_NOT_FOUND")
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
    /// Processes a refund
    /// </summary>
    /// <param name="createRefundDto">Refund processing data</param>
    /// <returns>Success result</returns>
    [HttpPost]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult> ProcessRefund([FromBody] CreateRefundDto createRefundDto)
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

        var command = new RefundCommand
        {
            VariantId = createRefundDto.VariantId,
            WarehouseId = createRefundDto.WarehouseId,
            Quantity = createRefundDto.Quantity,
            Reason = createRefundDto.Reason,
            OriginalSaleReference = createRefundDto.OriginalSaleReference,
            ActorId = userId,
            Metadata = createRefundDto.Metadata
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "REFUND_EXCEEDS_SALE")
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

            if (result.ErrorCode == "ORIGINAL_SALE_NOT_FOUND")
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

        return Ok(new { Message = "Refund processed successfully" });
    }

    /// <summary>
    /// Gets refund history for a sale reference
    /// </summary>
    /// <param name="saleReference">Original sale reference number</param>
    /// <returns>List of refund movements for the sale</returns>
    [HttpGet("history/{saleReference}")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<List<StockMovementDto>>> GetRefundHistory(string saleReference)
    {
        var query = new GetStockMovementHistoryQuery
        {
            ReferenceNumber = saleReference,
            MovementType = "Refund",
            Page = 1,
            PageSize = 100 // Get all refunds for this reference
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
    /// Gets all refund movements with filtering and pagination
    /// </summary>
    /// <param name="variantId">Optional variant filter</param>
    /// <param name="warehouseId">Optional warehouse filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paginated refund movements</returns>
    [HttpGet]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<PagedResult<StockMovementDto>>> GetRefunds(
        [FromQuery] Guid? variantId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetStockMovementHistoryQuery
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            MovementType = "Refund",
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
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
    /// Gets refund statistics for monitoring
    /// </summary>
    /// <param name="warehouseId">Optional warehouse filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <returns>Refund statistics</returns>
    [HttpGet("statistics")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<object>> GetRefundStatistics(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetStockMovementHistoryQuery
        {
            WarehouseId = warehouseId,
            MovementType = "Refund",
            FromDate = fromDate,
            ToDate = toDate,
            Page = 1,
            PageSize = 1000 // Get a large number for statistics calculation
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

        var refunds = result.Value.Items;

        // Calculate statistics
        var statistics = new
        {
            TotalRefunds = refunds.Count,
            TotalRefundedQuantity = refunds.Sum(r => r.Quantity),
            AverageRefundQuantity = refunds.Any() ? refunds.Average(r => r.Quantity) : 0,
            RefundsByDay = refunds
                .GroupBy(r => r.TimestampUtc.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count(),
                    TotalQuantity = g.Sum(r => r.Quantity)
                })
                .OrderBy(x => x.Date)
                .ToList(),
            TopRefundReasons = refunds
                .GroupBy(r => r.Reason)
                .Select(g => new
                {
                    Reason = g.Key,
                    Count = g.Count(),
                    TotalQuantity = g.Sum(r => r.Quantity)
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList(),
            CalculatedAtUtc = DateTime.UtcNow
        };

        return Ok(statistics);
    }

    /// <summary>
    /// Processes a partial refund with automatic quantity calculation
    /// </summary>
    /// <param name="request">Partial refund request</param>
    /// <returns>Success result</returns>
    [HttpPost("partial")]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult> ProcessPartialRefund([FromBody] PartialRefundRequest request)
    {
        // First validate the refund to get available quantity
        var validationQuery = new GetRefundValidationQuery
        {
            OriginalSaleReference = request.OriginalSaleReference,
            VariantId = request.VariantId,
            WarehouseId = request.WarehouseId,
            RequestedQuantity = request.RequestedQuantity
        };

        var validationResult = await _mediator.Send(validationQuery);

        if (!validationResult.IsSuccess)
        {
            return BadRequest(new
            {
                ErrorCode = validationResult.ErrorCode,
                Message = validationResult.ErrorMessage,
                Details = new Dictionary<string, object>(),
                TimestampUtc = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        if (!validationResult.Value.CanRefund)
        {
            return BadRequest(new
            {
                ErrorCode = "INVALID_REFUND_QUANTITY",
                Message = validationResult.Value.ValidationMessage,
                Details = new Dictionary<string, object>(),
                TimestampUtc = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Process the refund
        var createRefundDto = new CreateRefundDto
        {
            VariantId = request.VariantId,
            WarehouseId = request.WarehouseId,
            Quantity = request.RequestedQuantity,
            Reason = request.Reason,
            OriginalSaleReference = request.OriginalSaleReference,
            Metadata = request.Metadata
        };

        return await ProcessRefund(createRefundDto);
    }
}

/// <summary>
/// Request model for partial refund processing
/// </summary>
public class PartialRefundRequest
{
    public Guid VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string OriginalSaleReference { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}