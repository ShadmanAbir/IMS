using Dapper;
using Microsoft.Extensions.Logging;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Infrastructure.Data;

namespace IMS.Api.Infrastructure.Repositories;

/// <summary>
/// Optimized repository for high-performance inventory queries
/// Uses specialized SQL queries and indexes for bulk operations
/// </summary>
public class OptimizedInventoryRepository : IOptimizedInventoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<OptimizedInventoryRepository> _logger;

    public OptimizedInventoryRepository(
        IDbConnectionFactory connectionFactory,
        ITenantContext tenantContext,
        ILogger<OptimizedInventoryRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<InventoryItemDto>> GetBulkInventoryLevelsOptimizedAsync(
        List<Guid> variantIds,
        List<Guid>? warehouseIds = null,
        bool includeZeroStock = false,
        bool includeExpired = false)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var whereConditions = new List<string> { "ii.IsDeleted = false" };
        var parameters = GetTenantParameters();

        // Use ANY operator for efficient array lookups
        whereConditions.Add("ii.VariantId = ANY(@VariantIds)");
        parameters.Add("VariantIds", variantIds.ToArray());

        if (warehouseIds?.Any() == true)
        {
            whereConditions.Add("ii.WarehouseId = ANY(@WarehouseIds)");
            parameters.Add("WarehouseIds", warehouseIds.ToArray());
        }

        if (!includeZeroStock)
        {
            whereConditions.Add("ii.TotalStock > 0");
        }

        if (!includeExpired)
        {
            whereConditions.Add("(ii.ExpiryDate IS NULL OR ii.ExpiryDate > @CurrentDate)");
            parameters.Add("CurrentDate", DateTime.UtcNow);
        }

        var whereClause = string.Join(" AND ", whereConditions);

        // Optimized query using covering indexes
        var sql = $@"
            SELECT 
                ii.VariantId,
                v.Sku,
                v.Name as VariantName,
                ii.WarehouseId,
                w.Name as WarehouseName,
                ii.TotalStock,
                ii.ReservedStock,
                (ii.TotalStock - ii.ReservedStock) as AvailableStock,
                v.BaseUnitName as BaseUnit,
                ii.UpdatedAtUtc as LastUpdatedUtc,
                ii.ExpiryDate
            FROM InventoryItems ii
            INNER JOIN Variants v ON ii.VariantId = v.Id AND v.IsDeleted = false
            INNER JOIN Warehouses w ON ii.WarehouseId = w.Id AND w.IsDeleted = false
            WHERE {whereClause}
            ORDER BY v.Sku, w.Name";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await connection.QueryAsync<InventoryItemDto>(sql, parameters);
        stopwatch.Stop();

        _logger.LogDebug("Bulk inventory query completed in {ElapsedMs}ms for {VariantCount} variants", 
            stopwatch.ElapsedMilliseconds, variantIds.Count);

        return result.ToList();
    }

    public async Task<List<LowStockVariantDto>> GetLowStockVariantsOptimizedAsync(
        decimal lowStockThreshold,
        List<Guid>? warehouseIds = null,
        List<Guid>? categoryIds = null,
        int limit = 100)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var whereConditions = new List<string> 
        { 
            "ii.IsDeleted = false",
            "v.IsDeleted = false",
            "ii.TotalStock <= @LowStockThreshold",
            "ii.TotalStock >= 0" // Exclude negative stock from low stock alerts
        };
        
        var parameters = GetTenantParameters(new { LowStockThreshold = lowStockThreshold, Limit = limit });

        if (warehouseIds?.Any() == true)
        {
            whereConditions.Add("ii.WarehouseId = ANY(@WarehouseIds)");
            parameters.Add("WarehouseIds", warehouseIds.ToArray());
        }

        if (categoryIds?.Any() == true)
        {
            whereConditions.Add("p.CategoryId = ANY(@CategoryIds)");
            parameters.Add("CategoryIds", categoryIds.ToArray());
        }

        var whereClause = string.Join(" AND ", whereConditions);

        // Query optimized for low stock detection using specialized index
        var sql = $@"
            SELECT 
                ii.VariantId,
                v.Sku,
                v.Name as VariantName,
                ii.WarehouseId,
                w.Name as WarehouseName,
                ii.TotalStock as CurrentStock,
                @LowStockThreshold as LowStockThreshold,
                (@LowStockThreshold - ii.TotalStock) as StockDeficit,
                CASE 
                    WHEN ii.TotalStock = 0 THEN 'OutOfStock'
                    WHEN ii.TotalStock <= (@LowStockThreshold * 0.25) THEN 'Critical'
                    ELSE 'Low'
                END as Severity,
                ii.UpdatedAtUtc as LastUpdatedUtc,
                p.CategoryId,
                c.Name as CategoryName
            FROM InventoryItems ii
            INNER JOIN Variants v ON ii.VariantId = v.Id
            INNER JOIN Products p ON v.ProductId = p.Id AND p.IsDeleted = false
            INNER JOIN Warehouses w ON ii.WarehouseId = w.Id AND w.IsDeleted = false
            LEFT JOIN Categories c ON p.CategoryId = c.Id AND c.IsDeleted = false
            WHERE {whereClause}
            ORDER BY 
                CASE 
                    WHEN ii.TotalStock = 0 THEN 1
                    WHEN ii.TotalStock <= (@LowStockThreshold * 0.25) THEN 2
                    ELSE 3
                END,
                ii.TotalStock ASC,
                v.Sku
            LIMIT @Limit";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await connection.QueryAsync<LowStockVariantDto>(sql, parameters);
        stopwatch.Stop();

        _logger.LogDebug("Low stock query completed in {ElapsedMs}ms, found {Count} variants", 
            stopwatch.ElapsedMilliseconds, result.Count());

        return result.ToList();
    }

    public async Task<PagedResult<StockMovementDto>> GetStockMovementHistoryOptimizedAsync(
        Guid variantId,
        Guid? warehouseId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        List<string>? movementTypes = null,
        int page = 1,
        int pageSize = 50)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var whereConditions = new List<string> { "ii.IsDeleted = false" };
        var parameters = GetTenantParameters();

        whereConditions.Add("ii.VariantId = @VariantId");
        parameters.Add("VariantId", variantId);

        if (warehouseId.HasValue)
        {
            whereConditions.Add("ii.WarehouseId = @WarehouseId");
            parameters.Add("WarehouseId", warehouseId.Value);
        }

        if (fromDate.HasValue)
        {
            whereConditions.Add("sm.TimestampUtc >= @FromDate");
            parameters.Add("FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            whereConditions.Add("sm.TimestampUtc <= @ToDate");
            parameters.Add("ToDate", toDate.Value);
        }

        if (movementTypes?.Any() == true)
        {
            whereConditions.Add("sm.Type = ANY(@MovementTypes)");
            parameters.Add("MovementTypes", movementTypes.ToArray());
        }

        var whereClause = string.Join(" AND ", whereConditions);
        var offset = (page - 1) * pageSize;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        // Optimized query using covering index for stock movement history
        var sql = $@"
            -- Count query
            SELECT COUNT(*)
            FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            WHERE {whereClause};

            -- Data query with optimized ordering
            SELECT 
                sm.Id,
                sm.Type as MovementType,
                sm.Quantity,
                sm.RunningBalance,
                sm.Reason,
                u.FirstName + ' ' + u.LastName as ActorName,
                sm.TimestampUtc,
                sm.ReferenceNumber,
                sm.Metadata
            FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            LEFT JOIN AspNetUsers u ON sm.ActorId = u.Id
            WHERE {whereClause}
            ORDER BY sm.TimestampUtc DESC
            LIMIT @PageSize OFFSET @Offset";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        using var multi = await connection.QueryMultipleAsync(sql, parameters);

        var totalCount = await multi.ReadSingleAsync<int>();
        var movements = await multi.ReadAsync<StockMovementDto>();
        stopwatch.Stop();

        _logger.LogDebug("Stock movement history query completed in {ElapsedMs}ms for variant {VariantId}", 
          stopwatch.ElapsedMilliseconds, variantId);

        return new PagedResult<StockMovementDto>
        {
            Items = movements.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<Dictionary<Guid, decimal>> GetAvailableStockBatchAsync(
        List<(Guid VariantId, Guid WarehouseId)> variantWarehousePairs)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        if (!variantWarehousePairs.Any())
            return new Dictionary<Guid, decimal>();

        // Create a temporary table for the batch lookup
        var pairValues = string.Join(",", variantWarehousePairs.Select((pair, index) => 
            $"('{pair.VariantId}', '{pair.WarehouseId}')"));

        var parameters = GetTenantParameters();

        // Optimized batch query using VALUES clause
        var sql = $@"
            WITH VariantWarehousePairs AS (
                SELECT * FROM (VALUES {pairValues}) AS t(VariantId, WarehouseId)
            )
            SELECT 
                vwp.VariantId::uuid as VariantId,
                COALESCE(ii.TotalStock - ii.ReservedStock, 0) as AvailableStock
            FROM VariantWarehousePairs vwp
            LEFT JOIN InventoryItems ii ON 
                ii.VariantId = vwp.VariantId::uuid AND 
                ii.WarehouseId = vwp.WarehouseId::uuid AND 
                ii.IsDeleted = false
            WHERE ii.TenantId = @TenantId OR ii.TenantId IS NULL";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await connection.QueryAsync<(Guid VariantId, decimal AvailableStock)>(sql, parameters);
        stopwatch.Stop();

        _logger.LogDebug("Batch available stock query completed in {ElapsedMs}ms for {PairCount} pairs", 
            stopwatch.ElapsedMilliseconds, variantWarehousePairs.Count);

        return result.ToDictionary(x => x.VariantId, x => x.AvailableStock);
    }

    public async Task<List<InventoryItemDto>> GetExpiringInventoryAsync(
        DateTime expiryThreshold,
        List<Guid>? warehouseIds = null,
        int limit = 100)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var whereConditions = new List<string> 
        { 
            "ii.IsDeleted = false",
            "ii.ExpiryDate IS NOT NULL",
            "ii.ExpiryDate <= @ExpiryThreshold",
            "ii.TotalStock > 0"
        };
        
        var parameters = GetTenantParameters(new { ExpiryThreshold = expiryThreshold, Limit = limit });

        if (warehouseIds?.Any() == true)
        {
            whereConditions.Add("ii.WarehouseId = ANY(@WarehouseIds)");
            parameters.Add("WarehouseIds", warehouseIds.ToArray());
        }

        var whereClause = string.Join(" AND ", whereConditions);

        // Query optimized for expiry detection using specialized index
        var sql = $@"
            SELECT 
                ii.VariantId,
                v.Sku,
                v.Name as VariantName,
                ii.WarehouseId,
                w.Name as WarehouseName,
                ii.TotalStock,
                ii.ReservedStock,
                (ii.TotalStock - ii.ReservedStock) as AvailableStock,
                v.BaseUnitName as BaseUnit,
                ii.UpdatedAtUtc as LastUpdatedUtc,
                ii.ExpiryDate
            FROM InventoryItems ii
            INNER JOIN Variants v ON ii.VariantId = v.Id AND v.IsDeleted = false
            INNER JOIN Warehouses w ON ii.WarehouseId = w.Id AND w.IsDeleted = false
            WHERE {whereClause}
            ORDER BY ii.ExpiryDate ASC, ii.TotalStock DESC
            LIMIT @Limit";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await connection.QueryAsync<InventoryItemDto>(sql, parameters);
        stopwatch.Stop();

        _logger.LogDebug("Expiring inventory query completed in {ElapsedMs}ms, found {Count} items", 
            stopwatch.ElapsedMilliseconds, result.Count());

        return result.ToList();
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
}

/// <summary>
/// Interface for optimized inventory repository operations
/// </summary>
public interface IOptimizedInventoryRepository
{
    Task<List<InventoryItemDto>> GetBulkInventoryLevelsOptimizedAsync(
        List<Guid> variantIds,
        List<Guid>? warehouseIds = null,
        bool includeZeroStock = false,
        bool includeExpired = false);

    Task<List<LowStockVariantDto>> GetLowStockVariantsOptimizedAsync(
        decimal lowStockThreshold,
        List<Guid>? warehouseIds = null,
        List<Guid>? categoryIds = null,
        int limit = 100);

    Task<PagedResult<StockMovementDto>> GetStockMovementHistoryOptimizedAsync(
        Guid variantId,
        Guid? warehouseId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        List<string>? movementTypes = null,
        int page = 1,
        int pageSize = 50);

    Task<Dictionary<Guid, decimal>> GetAvailableStockBatchAsync(
        List<(Guid VariantId, Guid WarehouseId)> variantWarehousePairs);

    Task<List<InventoryItemDto>> GetExpiringInventoryAsync(
        DateTime expiryThreshold,
        List<Guid>? warehouseIds = null,
        int limit = 100);
}