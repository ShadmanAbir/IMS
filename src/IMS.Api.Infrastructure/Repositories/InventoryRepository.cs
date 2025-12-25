using AutoMapper;
using Dapper;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Queries.Inventory;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Infrastructure.Data;
using IMS.Api.Infrastructure.Data.DTOs;
using IMS.Api.Infrastructure.Repositories.Base;

namespace IMS.Api.Infrastructure.Repositories;

/// <summary>
/// Hybrid inventory repository implementing the EF Core + Dapper architectural pattern.
/// - Write operations (Create, Update, Delete) use EF Core with AutoMapper for strong consistency and transaction support
/// - Read operations use Dapper for optimized performance, especially for complex dashboard queries
/// - Automatically filters out soft-deleted records and applies tenant-aware access control
/// </summary>
public class InventoryRepository : IInventoryRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenantContext;

    public InventoryRepository(
        ApplicationDbContext context,
        IDbConnectionFactory connectionFactory, 
        IMapper mapper, 
        ITenantContext tenantContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    #region Write Operations (EF Core)

    /// <summary>
    /// Adds a new inventory item using EF Core for strong consistency
    /// </summary>
    /// <param name="entity">The inventory item to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
    public async Task AddAsync(InventoryItem entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _context.InventoryItems.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Updates an existing inventory item using EF Core for strong consistency
    /// </summary>
    /// <param name="entity">The inventory item to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
    public async Task UpdateAsync(InventoryItem entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        _context.InventoryItems.Update(entity);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Soft deletes an inventory item using EF Core for strong consistency
    /// </summary>
    /// <param name="entity">The inventory item to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
    public async Task DeleteAsync(InventoryItem entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        _context.InventoryItems.Remove(entity);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Saves all pending changes to the database using EF Core
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of affected records</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Read Operations (Dapper - Optimized for Performance)

    /// <summary>
    /// Retrieves an inventory item by its identifier using optimized Dapper query
    /// Automatically excludes soft-deleted records and applies tenant filtering
    /// </summary>
    /// <param name="id">The inventory item identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The inventory item if found, null otherwise</returns>
    public async Task<InventoryItem?> GetByIdAsync(InventoryItemId id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter(@"
            SELECT * FROM InventoryItems WHERE Id = @Id;
            SELECT * FROM StockMovements WHERE InventoryItemId = @Id ORDER BY TimestampUtc DESC;");

        var parameters = GetTenantParameters(new { Id = id.Value });

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        
        var inventoryDto = await multi.ReadSingleOrDefaultAsync<InventoryItemDto>();
        if (inventoryDto == null) return null;

        var movementDtos = await multi.ReadAsync<StockMovementDto>();
        
        var inventory = _mapper.Map<InventoryItem>(inventoryDto);
        
        // Note: Stock movements are loaded separately and would need to be added to the aggregate
        // This is a limitation of the current domain model design
        
        return inventory;
    }

    public async Task<IEnumerable<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter("SELECT * FROM InventoryItems");
        var parameters = GetTenantParameters();
        
        var inventoryDtos = await connection.QueryAsync<InventoryItemDto>(sql, parameters);
        
        return _mapper.Map<IEnumerable<InventoryItem>>(inventoryDtos);
    }

    public async Task<InventoryItem?> GetByVariantAndWarehouseAsync(VariantId variantId, WarehouseId warehouseId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter(@"
            SELECT * FROM InventoryItems 
            WHERE VariantId = @VariantId AND WarehouseId = @WarehouseId;
            
            SELECT sm.* FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            WHERE ii.VariantId = @VariantId AND ii.WarehouseId = @WarehouseId
            ORDER BY sm.TimestampUtc DESC;");

        var parameters = GetTenantParameters(new { 
            VariantId = variantId.Value, 
            WarehouseId = warehouseId.Value 
        });

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        
        var inventoryDto = await multi.ReadSingleOrDefaultAsync<InventoryItemDto>();
        if (inventoryDto == null) return null;

        var movementDtos = await multi.ReadAsync<StockMovementDto>();
        
        return _mapper.Map<InventoryItem>(inventoryDto);
    }

    public async Task<List<InventoryItem>> GetBulkInventoryAsync(List<VariantId> variantIds, WarehouseId? warehouseId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter(@"
            SELECT * FROM InventoryItems 
            WHERE VariantId IN @VariantIds AND WarehouseId = @WarehouseId");

        var parameters = GetTenantParameters(new { 
            VariantIds = variantIds.Select(v => v.Value).ToArray(),
            WarehouseId = warehouseId.Value,
            CancellationToken = cancellationToken
        });

        var inventoryDtos = await connection.QueryAsync<InventoryItemDto>(sql, parameters);
        
        return _mapper.Map<List<InventoryItem>>(inventoryDtos);
    }

    public async Task<List<InventoryItem>> GetBulkInventoryAsync(List<VariantId> variantIds, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter("SELECT * FROM InventoryItems WHERE VariantId IN @VariantIds");

        var parameters = GetTenantParameters(new { 
            VariantIds = variantIds.Select(v => v.Value).ToArray()
        });

        var inventoryDtos = await connection.QueryAsync<InventoryItemDto>(sql, parameters);
        
        return _mapper.Map<List<InventoryItem>>(inventoryDtos);
    }

    public async Task<bool> HasOpeningBalanceAsync(VariantId variantId, WarehouseId warehouseId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter(@"
            SELECT COUNT(1) FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            WHERE ii.VariantId = @VariantId AND ii.WarehouseId = @WarehouseId AND sm.Type = 'OpeningBalance'", "ii.TenantId");

        var parameters = GetTenantParameters(new { 
            VariantId = variantId.Value, 
            WarehouseId = warehouseId.Value 
        });

        var count = await connection.QuerySingleAsync<int>(sql, parameters);
        
        return count > 0;
    }

    public async Task<List<InventoryItem>> GetLowStockItemsAsync(WarehouseId? warehouseId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter("SELECT * FROM InventoryItems WHERE TotalStock <= 10 AND TotalStock > 0");
        var parameters = GetTenantParameters();

        if (warehouseId != null)
        {
            sql += " AND WarehouseId = @WarehouseId";
            parameters.Add("WarehouseId", warehouseId.Value);
        }

        sql += " ORDER BY TotalStock";

        var inventoryDtos = await connection.QueryAsync<InventoryItemDto>(sql, parameters);
        
        return _mapper.Map<List<InventoryItem>>(inventoryDtos);
    }

    public async Task<List<InventoryItem>> GetByWarehouseAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter("SELECT * FROM InventoryItems WHERE WarehouseId = @WarehouseId ORDER BY VariantId");

        var parameters = GetTenantParameters(new { 
            WarehouseId = warehouseId.Value 
        });

        var inventoryDtos = await connection.QueryAsync<InventoryItemDto>(sql, parameters);
        
        return _mapper.Map<List<InventoryItem>>(inventoryDtos);
    }

    /// <summary>
    /// Gets inventory items that are expired or near expiry
    /// </summary>
    /// <param name="daysThreshold">Number of days to consider as "near expiry"</param>
    /// <param name="warehouseId">Optional warehouse filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of inventory items near or past expiry</returns>
    public async Task<List<InventoryItem>> GetExpiringItemsAsync(int daysThreshold = 7, WarehouseId? warehouseId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);
        var sql = AddTenantFilter(@"
            SELECT * FROM InventoryItems 
            WHERE ExpiryDate IS NOT NULL 
            AND ExpiryDate <= @ThresholdDate 
            AND TotalStock > 0");
        
        var parameters = GetTenantParameters(new { ThresholdDate = thresholdDate });

        if (warehouseId != null)
        {
            sql += " AND WarehouseId = @WarehouseId";
            parameters.Add("WarehouseId", warehouseId.Value);
        }

        sql += " ORDER BY ExpiryDate ASC";

        var inventoryDtos = await connection.QueryAsync<InventoryItemDto>(sql, parameters);
        
        return _mapper.Map<List<InventoryItem>>(inventoryDtos);
    }

    /// <summary>
    /// Gets expired inventory items with stock
    /// </summary>
    /// <param name="warehouseId">Optional warehouse filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expired inventory items</returns>
    public async Task<List<InventoryItem>> GetExpiredItemsAsync(WarehouseId? warehouseId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter(@"
            SELECT * FROM InventoryItems 
            WHERE ExpiryDate IS NOT NULL 
            AND ExpiryDate <= @CurrentDate 
            AND TotalStock > 0");
        
        var parameters = GetTenantParameters(new { CurrentDate = DateTime.UtcNow });

        if (warehouseId != null)
        {
            sql += " AND WarehouseId = @WarehouseId";
            parameters.Add("WarehouseId", warehouseId.Value);
        }

        sql += " ORDER BY ExpiryDate ASC";

        var inventoryDtos = await connection.QueryAsync<InventoryItemDto>(sql, parameters);
        
        return _mapper.Map<List<InventoryItem>>(inventoryDtos);
    }

    /// <summary>
    /// Gets inventory items ordered by expiry date for FIFO/FEFO processing
    /// </summary>
    /// <param name="variantId">The variant to get inventory for</param>
    /// <param name="warehouseId">Optional warehouse filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of inventory items ordered by expiry date (FIFO/FEFO)</returns>
    public async Task<List<InventoryItem>> GetInventoryByExpiryOrderAsync(VariantId variantId, WarehouseId? warehouseId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter(@"
            SELECT * FROM InventoryItems 
            WHERE VariantId = @VariantId 
            AND TotalStock > 0");
        
        var parameters = GetTenantParameters(new { VariantId = variantId.Value });

        if (warehouseId != null)
        {
            sql += " AND WarehouseId = @WarehouseId";
            parameters.Add("WarehouseId", warehouseId.Value);
        }

        // Order by expiry date (nulls last for non-perishable items)
        sql += " ORDER BY ExpiryDate ASC NULLS LAST, UpdatedAtUtc ASC";

        var inventoryDtos = await connection.QueryAsync<InventoryItemDto>(sql, parameters);
        
        return _mapper.Map<List<InventoryItem>>(inventoryDtos);
    }

    /// <summary>
    /// Adds tenant filter to WHERE clause if tenant context is available
    /// Also adds soft delete filter to exclude deleted records using PostgreSQL syntax
    /// </summary>
    private string AddTenantFilter(string baseQuery, string tenantColumn = "TenantId")
    {
        var filters = new List<string>();
        
        // Add soft delete filter using PostgreSQL boolean syntax
        filters.Add("IsDeleted = false");
        
        // Add tenant filter if available
        if (_tenantContext.CurrentTenantId != null)
        {
            filters.Add($"{tenantColumn} = @TenantId");
        }
        
        var combinedFilter = string.Join(" AND ", filters);
        
        if (baseQuery.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
            return baseQuery.Replace("WHERE", $"WHERE {combinedFilter} AND (") + ")";
        
        return $"{baseQuery} WHERE {combinedFilter}";
    }

    /// <summary>
    /// Gets tenant parameter for queries
    /// </summary>
    private DynamicParameters GetTenantParameters(object? additionalParams = null)
    {
        var parameters = new DynamicParameters(additionalParams);
        
        if (_tenantContext.CurrentTenantId != null)
        {
            parameters.Add("TenantId", _tenantContext.CurrentTenantId.Value);
        }
        
        return parameters;
    }

    Task<List<LowStockVariantResult>> IInventoryRepository.GetLowStockVariantsAsync(LowStockQuery query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Helper Methods

    #endregion
}