using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Warehouse aggregate
/// </summary>
public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Warehouse>> GetByIdsAsync(List<WarehouseId> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Warehouse entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Warehouse entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Warehouse entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a warehouse by its code
    /// </summary>
    /// <param name="code">Warehouse code to search for</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Warehouse with the specified code, or null if not found</returns>
    Task<Warehouse?> GetByCodeAsync(string code, TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active warehouses for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active warehouses</returns>
    Task<List<Warehouse>> GetActiveWarehousesAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of warehouses with optional filtering
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchTerm">Optional search term to filter by name, code, or city</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="includeDeleted">Whether to include soft-deleted warehouses</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of warehouses</returns>
    Task<PagedResult<Warehouse>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        bool? isActive = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a warehouse code already exists for the tenant
    /// </summary>
    /// <param name="code">Warehouse code to check</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="excludeWarehouseId">Optional warehouse ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the code exists, false otherwise</returns>
    Task<bool> ExistsWithCodeAsync(
        string code,
        TenantId tenantId,
        WarehouseId? excludeWarehouseId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a warehouse name already exists for the tenant
    /// </summary>
    /// <param name="name">Warehouse name to check</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="excludeWarehouseId">Optional warehouse ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the name exists, false otherwise</returns>
    Task<bool> ExistsWithNameAsync(
        string name,
        TenantId tenantId,
        WarehouseId? excludeWarehouseId = null,
        CancellationToken cancellationToken = default);
}