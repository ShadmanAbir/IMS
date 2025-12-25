using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Variant entity
/// </summary>
public interface IVariantRepository
{
    Task<Variant?> GetByIdAsync(VariantId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Variant>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Variant>> GetByIdsAsync(List<VariantId> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Variant entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Variant entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Variant entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a variant by its SKU
    /// </summary>
    /// <param name="sku">SKU to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Variant with the specified SKU, or null if not found</returns>
    Task<Variant?> GetBySkuAsync(SKU sku, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets variants by product ID
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="includeDeleted">Whether to include soft-deleted variants</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of variants for the product</returns>
    Task<List<Variant>> GetByProductAsync(
        ProductId productId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of variants with optional filtering
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchTerm">Optional search term to filter by name or SKU</param>
    /// <param name="productId">Optional product filter</param>
    /// <param name="sku">Optional SKU filter</param>
    /// <param name="includeDeleted">Whether to include soft-deleted variants</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of variants</returns>
    Task<PagedResult<Variant>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        ProductId? productId = null,
        string? sku = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a SKU already exists
    /// </summary>
    /// <param name="sku">SKU to check</param>
    /// <param name="excludeVariantId">Optional variant ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the SKU exists, false otherwise</returns>
    Task<bool> ExistsWithSkuAsync(
        SKU sku,
        VariantId? excludeVariantId = null,
        CancellationToken cancellationToken = default);
}