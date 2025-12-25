using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using IMS.Api.Application.Commands.Variants;
using IMS.Api.Application.Queries.Variants;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Controller for managing product variants
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class VariantsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<VariantsController> _logger;

    public VariantsController(IMediator mediator, ILogger<VariantsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of variants
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="productId">Optional product filter</param>
    /// <param name="sku">Optional SKU filter</param>
    /// <param name="includeDeleted">Include soft-deleted variants (default: false)</param>
    /// <returns>Paginated list of variants</returns>
    [HttpGet]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<PagedResult<VariantDto>>> GetVariants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] string? sku = null,
        [FromQuery] bool includeDeleted = false)
    {
        var query = new GetVariantsQuery
        {
            Page = page,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            ProductId = productId,
            Sku = sku,
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
    /// Gets a specific variant by ID
    /// </summary>
    /// <param name="id">Variant ID</param>
    /// <returns>Variant details</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<VariantDto>> GetVariant(Guid id)
    {
        var query = new GetVariantQuery { Id = id };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "VARIANT_NOT_FOUND")
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
    /// Gets a variant by SKU
    /// </summary>
    /// <param name="sku">Variant SKU</param>
    /// <returns>Variant details</returns>
    [HttpGet("by-sku/{sku}")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<VariantDto>> GetVariantBySku(string sku)
    {
        var query = new GetVariantsQuery
        {
            Sku = sku,
            Page = 1,
            PageSize = 1
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

        if (!result.Value.Items.Any())
        {
            return NotFound(new
            {
                ErrorCode = "VARIANT_NOT_FOUND",
                Message = $"Variant with SKU '{sku}' not found",
                Details = new Dictionary<string, object>(),
                TimestampUtc = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(result.Value.Items.First());
    }

    /// <summary>
    /// Creates a new variant
    /// </summary>
    /// <param name="createVariantDto">Variant creation data</param>
    /// <returns>Created variant</returns>
    [HttpPost]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<VariantDto>> CreateVariant([FromBody] CreateVariantDto createVariantDto)
    {
        var command = new CreateVariantCommand
        {
            Sku = createVariantDto.Sku,
            Name = createVariantDto.Name,
            BaseUnit = createVariantDto.BaseUnit,
            ProductId = createVariantDto.ProductId,
            Attributes = createVariantDto.Attributes,
            UnitConversions = createVariantDto.UnitConversions
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "DUPLICATE_SKU")
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

            if (result.ErrorCode == "PRODUCT_NOT_FOUND")
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
            nameof(GetVariant),
            new { id = result.Value.Id },
            result.Value);
    }

    /// <summary>
    /// Updates an existing variant
    /// </summary>
    /// <param name="id">Variant ID</param>
    /// <param name="updateVariantDto">Variant update data</param>
    /// <returns>Updated variant</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<VariantDto>> UpdateVariant(
        Guid id,
        [FromBody] UpdateVariantDto updateVariantDto)
    {
        var command = new UpdateVariantCommand
        {
            Id = id,
            Name = updateVariantDto.Name
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "VARIANT_NOT_FOUND")
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
    /// Soft deletes a variant
    /// </summary>
    /// <param name="id">Variant ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult> DeleteVariant(Guid id)
    {
        // Get the current user ID from claims (this would typically come from JWT token)
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

        var command = new DeleteVariantCommand
        {
            Id = id,
            DeletedBy = userId
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "VARIANT_NOT_FOUND")
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
    /// Adds an attribute to a variant
    /// </summary>
    /// <param name="id">Variant ID</param>
    /// <param name="attributeDto">Attribute data</param>
    /// <returns>Updated variant</returns>
    [HttpPost("{id:guid}/attributes")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<VariantDto>> AddVariantAttribute(
        Guid id,
        [FromBody] CreateVariantAttributeDto attributeDto)
    {
        // This would require a separate command - AddVariantAttributeCommand
        // For now, return a placeholder response
        return BadRequest(new
        {
            ErrorCode = "NOT_IMPLEMENTED",
            Message = "Adding individual attributes is not yet implemented. Use the create variant endpoint with attributes.",
            Details = new Dictionary<string, object>(),
            TimestampUtc = DateTime.UtcNow,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    /// <summary>
    /// Updates a variant attribute
    /// </summary>
    /// <param name="id">Variant ID</param>
    /// <param name="attributeName">Attribute name</param>
    /// <param name="updateAttributeDto">Attribute update data</param>
    /// <returns>Updated variant</returns>
    [HttpPut("{id:guid}/attributes/{attributeName}")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<VariantDto>> UpdateVariantAttribute(
        Guid id,
        string attributeName,
        [FromBody] UpdateAttributeDto updateAttributeDto)
    {
        // This would require a separate command - UpdateVariantAttributeCommand
        // For now, return a placeholder response
        return BadRequest(new
        {
            ErrorCode = "NOT_IMPLEMENTED",
            Message = "Updating individual attributes is not yet implemented.",
            Details = new Dictionary<string, object>(),
            TimestampUtc = DateTime.UtcNow,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    /// <summary>
    /// Removes an attribute from a variant
    /// </summary>
    /// <param name="id">Variant ID</param>
    /// <param name="attributeName">Attribute name</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}/attributes/{attributeName}")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult> RemoveVariantAttribute(Guid id, string attributeName)
    {
        // This would require a separate command - RemoveVariantAttributeCommand
        // For now, return a placeholder response
        return BadRequest(new
        {
            ErrorCode = "NOT_IMPLEMENTED",
            Message = "Removing individual attributes is not yet implemented.",
            Details = new Dictionary<string, object>(),
            TimestampUtc = DateTime.UtcNow,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    /// <summary>
    /// Adds a unit conversion to a variant
    /// </summary>
    /// <param name="id">Variant ID</param>
    /// <param name="conversionDto">Unit conversion data</param>
    /// <returns>Updated variant</returns>
    [HttpPost("{id:guid}/unit-conversions")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<VariantDto>> AddUnitConversion(
        Guid id,
        [FromBody] CreateUnitConversionDto conversionDto)
    {
        // This would require a separate command - AddUnitConversionCommand
        // For now, return a placeholder response
        return BadRequest(new
        {
            ErrorCode = "NOT_IMPLEMENTED",
            Message = "Adding individual unit conversions is not yet implemented. Use the create variant endpoint with conversions.",
            Details = new Dictionary<string, object>(),
            TimestampUtc = DateTime.UtcNow,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}