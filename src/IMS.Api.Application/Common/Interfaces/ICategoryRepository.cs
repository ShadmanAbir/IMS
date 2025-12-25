using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Category entity
/// </summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Category>> GetByIdsAsync(List<CategoryId> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Category entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Category entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Category entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets root categories (categories without parent)
    /// </summary>
    /// <param name="includeDeleted">Whether to include soft-deleted categories</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of root categories</returns>
    Task<List<Category>> GetRootCategoriesAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child categories of a parent category
    /// </summary>
    /// <param name="parentCategoryId">Parent category identifier</param>
    /// <param name="includeDeleted">Whether to include soft-deleted categories</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child categories</returns>
    Task<List<Category>> GetChildCategoriesAsync(
        CategoryId parentCategoryId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of categories with optional filtering
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchTerm">Optional search term to filter by name or code</param>
    /// <param name="parentCategoryId">Optional parent category filter</param>
    /// <param name="includeDeleted">Whether to include soft-deleted categories</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of categories</returns>
    Task<PagedResult<Category>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        CategoryId? parentCategoryId = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a category name already exists for the tenant at the same level
    /// </summary>
    /// <param name="name">Category name to check</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="parentCategoryId">Parent category identifier (null for root level)</param>
    /// <param name="excludeCategoryId">Optional category ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the name exists, false otherwise</returns>
    Task<bool> ExistsWithNameAsync(
        string name,
        TenantId tenantId,
        CategoryId? parentCategoryId = null,
        CategoryId? excludeCategoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a category code already exists for the tenant
    /// </summary>
    /// <param name="code">Category code to check</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="excludeCategoryId">Optional category ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the code exists, false otherwise</returns>
    Task<bool> ExistsWithCodeAsync(
        string code,
        TenantId tenantId,
        CategoryId? excludeCategoryId = null,
        CancellationToken cancellationToken = default);
}