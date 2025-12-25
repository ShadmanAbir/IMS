using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Product aggregate
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Product>> GetByIdsAsync(List<ProductId> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Product entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Product entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a paginated list of products with optional filtering
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchTerm">Optional search term to filter by name or description</param>
    /// <param name="categoryId">Optional category filter</param>
    /// <param name="includeDeleted">Whether to include soft-deleted products</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of products</returns>
    Task<PagedResult<Product>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        CategoryId? categoryId = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products by category
    /// </summary>
    /// <param name="categoryId">Category identifier</param>
    /// <param name="includeDeleted">Whether to include soft-deleted products</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products in the category</returns>
    Task<List<Product>> GetByCategoryAsync(
        CategoryId categoryId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product name already exists for the tenant
    /// </summary>
    /// <param name="name">Product name to check</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="excludeProductId">Optional product ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the name exists, false otherwise</returns>
    Task<bool> ExistsWithNameAsync(
        string name,
        TenantId tenantId,
        ProductId? excludeProductId = null,
        CancellationToken cancellationToken = default);
}