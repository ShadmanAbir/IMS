using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using IMS.Api.Application.Queries.Inventory;
using IMS.Api.Application.Commands.StockMovements;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Controller for managing inventory levels and stock queries
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(IMediator mediator, ILogger<InventoryController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets inventory level for a specific variant in a warehouse
    /// </summary>
    /// <param name="variantId">Variant ID</param>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <returns>Inventory level details</returns>
    [HttpGet("{variantId:guid}/warehouse/{warehouseId:guid}")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<InventoryItemDto>> GetInventoryLevel(Guid variantId, Guid warehouseId)
    {
        var query = new GetInventoryLevelQuery
        {
            VariantId = variantId,
            WarehouseId = warehouseId
        };

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "INVENTORY_NOT_FOUND")
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
    /// Gets inventory levels for multiple variants
    /// </summary>
    /// <param name="request">Bulk inventory query request</param>
    /// <returns>List of inventory levels</returns>
    [HttpPost("bulk")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<List<InventoryItemDto>>> GetBulkInventoryLevels([FromBody] GetBulkInventoryLevelsQuery request)
    {
        var result = await _mediator.Send(request);

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
    /// Gets inventory levels with filtering and pagination
    /// </summary>
    /// <param name="warehouseId">Optional warehouse filter</param>
    /// <param name="includeExpired">Include expired items (default: true)</param>
    /// <param name="includeOutOfStock">Include out of stock items (default: true)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paginated inventory levels</returns>
    [HttpGet]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<List<InventoryItemDto>>> GetInventoryLevels(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] bool includeExpired = true,
        [FromQuery] bool includeOutOfStock = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // For now, we'll use the bulk query with empty variant IDs to get all
        // In a real implementation, this would need a separate paginated query
        var query = new GetBulkInventoryLevelsQuery
        {
            VariantIds = new List<Guid>(), // Empty means get all
            WarehouseId = warehouseId,
            IncludeExpired = includeExpired,
            IncludeOutOfStock = includeOutOfStock
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

        // Apply pagination manually (in a real implementation, this should be done at the query level)
        var pagedItems = result.Value
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(pagedItems);
    }

    /// <summary>
    /// Gets low stock variants with configurable thresholds
    /// </summary>
    /// <param name="warehouseId">Optional warehouse filter</param>
    /// <param name="threshold">Low stock threshold (default: 10)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>List of low stock variants</returns>
    [HttpGet("low-stock")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<List<LowStockVariantDto>>> GetLowStockVariants(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] decimal threshold = 10,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetLowStockVariantsQuery
        {
            WarehouseId = warehouseId,
            CustomThreshold = threshold,
            MaxResults = Math.Min(pageSize, 500),
            IncludeOutOfStock = true
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

        // Handler returns a simple list (not paged). Apply page manually if needed.
        var items = result.Value
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(items);
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
}