using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using IMS.Api.Application.Commands.Products;
using IMS.Api.Application.Queries.Products;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Controller for managing products
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of products
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="categoryId">Optional category filter</param>
    /// <param name="includeDeleted">Include soft-deleted products (default: false)</param>
    /// <param name="includeVariants">Include variant details (default: false)</param>
    /// <returns>Paginated list of products</returns>
    [HttpGet]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool includeVariants = false)
    {
        var query = new GetProductsQuery
        {
            Page = page,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            CategoryId = categoryId,
            IncludeDeleted = includeDeleted,
            IncludeVariants = includeVariants
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
    /// Gets a specific product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="includeVariants">Include variant details (default: false)</param>
    /// <returns>Product details</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<ProductDto>> GetProduct(
        Guid id,
        [FromQuery] bool includeVariants = false)
    {
        var query = new GetProductQuery
        {
            Id = id,
            IncludeVariants = includeVariants
        };

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "PRODUCT_NOT_FOUND")
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
    /// Creates a new product
    /// </summary>
    /// <param name="createProductDto">Product creation data</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createProductDto)
    {
        var command = new CreateProductCommand
        {
            Name = createProductDto.Name,
            Description = createProductDto.Description,
            CategoryId = createProductDto.CategoryId,
            Attributes = createProductDto.Attributes
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "DUPLICATE_NAME")
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
            nameof(GetProduct),
            new { id = result.Value.Id },
            result.Value);
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="updateProductDto">Product update data</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductDto updateProductDto)
    {
        var command = new UpdateProductCommand
        {
            Id = id,
            Name = updateProductDto.Name,
            Description = updateProductDto.Description,
            CategoryId = updateProductDto.CategoryId
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "PRODUCT_NOT_FOUND")
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
    /// Soft deletes a product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult> DeleteProduct(Guid id)
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

        var command = new DeleteProductCommand
        {
            Id = id,
            DeletedBy = userId
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "PRODUCT_NOT_FOUND")
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
    /// Adds an attribute to a product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="attributeDto">Attribute data</param>
    /// <returns>Updated product</returns>
    [HttpPost("{id:guid}/attributes")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<ProductDto>> AddProductAttribute(
        Guid id,
        [FromBody] CreateProductAttributeDto attributeDto)
    {
        // This would require a separate command - AddProductAttributeCommand
        // For now, return a placeholder response
        return BadRequest(new
        {
            ErrorCode = "NOT_IMPLEMENTED",
            Message = "Adding individual attributes is not yet implemented. Use the update product endpoint.",
            Details = new Dictionary<string, object>(),
            TimestampUtc = DateTime.UtcNow,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    /// <summary>
    /// Updates a product attribute
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="attributeName">Attribute name</param>
    /// <param name="updateAttributeDto">Attribute update data</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id:guid}/attributes/{attributeName}")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<ProductDto>> UpdateProductAttribute(
        Guid id,
        string attributeName,
        [FromBody] UpdateAttributeDto updateAttributeDto)
    {
        // This would require a separate command - UpdateProductAttributeCommand
        // For now, return a placeholder response
        return BadRequest(new
        {
            ErrorCode = "NOT_IMPLEMENTED",
            Message = "Updating individual attributes is not yet implemented. Use the update product endpoint.",
            Details = new Dictionary<string, object>(),
            TimestampUtc = DateTime.UtcNow,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    /// <summary>
    /// Removes an attribute from a product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="attributeName">Attribute name</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}/attributes/{attributeName}")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult> RemoveProductAttribute(Guid id, string attributeName)
    {
        // This would require a separate command - RemoveProductAttributeCommand
        // For now, return a placeholder response
        return BadRequest(new
        {
            ErrorCode = "NOT_IMPLEMENTED",
            Message = "Removing individual attributes is not yet implemented. Use the update product endpoint.",
            Details = new Dictionary<string, object>(),
            TimestampUtc = DateTime.UtcNow,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}