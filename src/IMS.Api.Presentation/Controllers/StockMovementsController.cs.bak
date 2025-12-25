using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using IMS.Api.Application.Queries.Inventory;
using IMS.Api.Application.Commands.StockMovements;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Controller for managing stock movements and history
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class StockMovementsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StockMovementsController> _logger;

    public StockMovementsController(IMediator mediator, ILogger<StockMovementsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets stock movement history with filtering and pagination
    /// </summary>
    /// <param name="variantId">Optional variant filter</param>
    /// <param name="warehouseId">Optional warehouse filter</param>
    /// <param name="movementType">Optional movement type filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paginated stock movement history</returns>
    [HttpGet]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<PagedResult<StockMovementDto>>> GetStockMovementHistory(
        [FromQuery] Guid? variantId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] string? movementType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetStockMovementHistoryQuery
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            MovementType = movementType,
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
    /// Gets stock movements by reference number
    /// </summary>
    /// <param name="referenceNumber">Reference number to search for</param>
    /// <returns>List of stock movements with the reference number</returns>
    [HttpGet("by-reference/{referenceNumber}")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<List<StockMovementDto>>> GetStockMovementsByReference(string referenceNumber)
    {
        var query = new GetStockMovementHistoryQuery
        {
            ReferenceNumber = referenceNumber,
            Page = 1,
            PageSize = 100 // Get all movements for this reference
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
    /// Records a purchase (stock increase)
    /// </summary>
    /// <param name="command">Purchase command</param>
    /// <returns>Success result</returns>
    [HttpPost("purchase")]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult> RecordPurchase([FromBody] PurchaseCommand command)
    {
        // Get the current user ID from claims
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            command.ActorId = userId;
        }

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "INVENTORY_NOT_FOUND")
            {
                return BadRequest(new
                {
                    ErrorCode = result.ErrorCode,
                    Message = "Inventory item not found. Please set opening balance first.",
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

        return Ok(new { Message = "Purchase recorded successfully" });
    }

    /// <summary>
    /// Records a sale (stock decrease)
    /// </summary>
    /// <param name="command">Sale command</param>
    /// <returns>Success result</returns>
    [HttpPost("sale")]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult> RecordSale([FromBody] SaleCommand command)
    {
        // Get the current user ID from claims
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            command.ActorId = userId;
        }

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "OUT_OF_STOCK" || result.ErrorCode == "INSUFFICIENT_STOCK")
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

        return Ok(new { Message = "Sale recorded successfully" });
    }

    /// <summary>
    /// Records a stock adjustment
    /// </summary>
    /// <param name="command">Adjustment command</param>
    /// <returns>Success result</returns>
    [HttpPost("adjustment")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult> RecordAdjustment([FromBody] AdjustmentCommand command)
    {
        // Get the current user ID from claims
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            command.ActorId = userId;
        }

        var result = await _mediator.Send(command);

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

        return Ok(new { Message = "Stock adjustment recorded successfully" });
    }

    /// <summary>
    /// Records a stock transfer between warehouses
    /// </summary>
    /// <param name="command">Transfer command</param>
    /// <returns>Success result</returns>
    [HttpPost("transfer")]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult> RecordTransfer([FromBody] TransferCommand command)
    {
        // Get the current user ID from claims
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            command.ActorId = userId;
        }

        var result = await _mediator.Send(command);

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

        return Ok(new { Message = "Stock transfer recorded successfully" });
    }

    /// <summary>
    /// Records a refund (stock increase from returned items)
    /// </summary>
    /// <param name="command">Refund command</param>
    /// <returns>Success result</returns>
    [HttpPost("refund")]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult> RecordRefund([FromBody] RefundCommand command)
    {
        // Get the current user ID from claims
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            command.ActorId = userId;
        }

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

            return BadRequest(new
            {
                ErrorCode = result.ErrorCode,
                Message = result.ErrorMessage,
                Details = new Dictionary<string, object>(),
                TimestampUtc = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(new { Message = "Refund recorded successfully" });
    }

    /// <summary>
    /// Sets the opening balance for a variant in a warehouse
    /// </summary>
    /// <param name="command">Opening balance command</param>
    /// <returns>Success result</returns>
    [HttpPost("opening-balance")]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult> SetOpeningBalance([FromBody] OpeningBalanceCommand command)
    {
        // Get the current user ID from claims
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            command.ActorId = userId;
        }

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "OPENING_BALANCE_EXISTS")
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

        return Ok(new { Message = "Opening balance set successfully" });
    }
}