using AutoMapper;
using Dapper;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Infrastructure.Data;

namespace IMS.Api.Infrastructure.Repositories;

/// <summary>
/// Dashboard repository implementing optimized read operations using Dapper for real-time metrics
/// Focuses on complex aggregation queries for dashboard functionality
/// </summary>
public class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenantContext;

    public DashboardRepository(
        IDbConnectionFactory connectionFactory,
        IMapper mapper,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<DashboardMetricsDto> GetRealTimeMetricsAsync(
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool includeExpired = false,
        decimal? lowStockThreshold = null)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var whereConditions = new List<string> { "ii.IsDeleted = false" };
        var parameters = GetTenantParameters();

        if (warehouseIds?.Any() == true)
        {
            whereConditions.Add("ii.WarehouseId = ANY(@WarehouseIds)");
            parameters.Add("WarehouseIds", warehouseIds.ToArray());
        }

        if (variantIds?.Any() == true)
        {
            whereConditions.Add("ii.VariantId = ANY(@VariantIds)");
            parameters.Add("VariantIds", variantIds.ToArray());
        }

        if (!includeExpired)
        {
            whereConditions.Add("(ii.ExpiryDate IS NULL OR ii.ExpiryDate > @CurrentDate)");
            parameters.Add("CurrentDate", DateTime.UtcNow);
        }

        var whereClause = string.Join(" AND ", whereConditions);
        var lowStockThresholdValue = lowStockThreshold ?? 10m;
        parameters.Add("LowStockThreshold", lowStockThresholdValue);

        var sql = $@"
            -- Main metrics
            SELECT 
                COALESCE(SUM(ii.TotalStock), 0) as TotalStockValue,
                COALESCE(SUM(ii.TotalStock - ii.ReservedStock), 0) as TotalAvailableStock,
                COALESCE(SUM(ii.ReservedStock), 0) as TotalReservedStock,
                COUNT(CASE WHEN ii.TotalStock <= @LowStockThreshold AND ii.TotalStock > 0 THEN 1 END) as LowStockVariantCount,
                COUNT(CASE WHEN ii.TotalStock <= 0 THEN 1 END) as OutOfStockVariantCount,
                COUNT(CASE WHEN ii.ExpiryDate IS NOT NULL AND ii.ExpiryDate <= @CurrentDate THEN 1 END) as ExpiredVariantCount,
                COUNT(CASE WHEN ii.ExpiryDate IS NOT NULL AND ii.ExpiryDate <= @CurrentDate + INTERVAL '7 days' AND ii.ExpiryDate > @CurrentDate THEN 1 END) as ExpiringVariantCount
            FROM InventoryItems ii
            LEFT JOIN Warehouses w ON ii.WarehouseId = w.Id
            WHERE {whereClause};

            -- Warehouse breakdown
            SELECT 
                w.Id as WarehouseId,
                w.Name as WarehouseName,
                w.Location,
                COALESCE(SUM(ii.TotalStock), 0) as TotalStock,
                COALESCE(SUM(ii.TotalStock - ii.ReservedStock), 0) as AvailableStock,
                COALESCE(SUM(ii.ReservedStock), 0) as ReservedStock,
                COUNT(ii.Id) as VariantCount,
                COUNT(CASE WHEN ii.TotalStock <= @LowStockThreshold AND ii.TotalStock > 0 THEN 1 END) as LowStockVariantCount,
                COUNT(CASE WHEN ii.TotalStock <= 0 THEN 1 END) as OutOfStockVariantCount,
                COUNT(CASE WHEN ii.ExpiryDate IS NOT NULL AND ii.ExpiryDate <= @CurrentDate THEN 1 END) as ExpiredVariantCount,
                MAX(ii.UpdatedAtUtc) as LastUpdatedUtc
            FROM Warehouses w
            LEFT JOIN InventoryItems ii ON w.Id = ii.WarehouseId AND ii.IsDeleted = false
            WHERE w.IsDeleted = false {(warehouseIds?.Any() == true ? "AND w.Id = ANY(@WarehouseIds)" : "")}
            GROUP BY w.Id, w.Name, w.Location
            ORDER BY w.Name;";

        using var multi = await connection.QueryMultipleAsync(sql, parameters);

        var mainMetrics = await multi.ReadSingleAsync();
        var warehouseBreakdown = await multi.ReadAsync<WarehouseStockDto>();

        // Get movement rates for the last 24 hours
        var movementRates = await GetStockMovementRatesAsync(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            warehouseIds,
            variantIds);

        return new DashboardMetricsDto
        {
            TotalStockValue = mainMetrics.TotalStockValue,
            TotalAvailableStock = mainMetrics.TotalAvailableStock,
            TotalReservedStock = mainMetrics.TotalReservedStock,
            LowStockVariantCount = mainMetrics.LowStockVariantCount,
            OutOfStockVariantCount = mainMetrics.OutOfStockVariantCount,
            ExpiredVariantCount = mainMetrics.ExpiredVariantCount,
            ExpiringVariantCount = mainMetrics.ExpiringVariantCount,
            WarehouseBreakdown = warehouseBreakdown.ToList(),
            MovementRates = movementRates,
            GeneratedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<List<WarehouseStockDto>> GetWarehouseStockLevelsAsync(
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        bool includeExpired = false,
        decimal? lowStockThreshold = null,
        bool includeEmptyWarehouses = true)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var whereConditions = new List<string> { "w.IsDeleted = false" };
        var parameters = GetTenantParameters();

        if (warehouseIds?.Any() == true)
        {
            whereConditions.Add("w.Id = ANY(@WarehouseIds)");
            parameters.Add("WarehouseIds", warehouseIds.ToArray());
        }

        var inventoryConditions = new List<string> { "ii.IsDeleted = false" };

        if (variantIds?.Any() == true)
        {
            inventoryConditions.Add("ii.VariantId = ANY(@VariantIds)");
            parameters.Add("VariantIds", variantIds.ToArray());
        }

        if (!includeExpired)
        {
            inventoryConditions.Add("(ii.ExpiryDate IS NULL OR ii.ExpiryDate > @CurrentDate)");
            parameters.Add("CurrentDate", DateTime.UtcNow);
        }

        if (!includeEmptyWarehouses)
        {
            inventoryConditions.Add("ii.TotalStock > 0");
        }

        var lowStockThresholdValue = lowStockThreshold ?? 10m;
        parameters.Add("LowStockThreshold", lowStockThresholdValue);

        var whereClause = string.Join(" AND ", whereConditions);
        var inventoryWhereClause = string.Join(" AND ", inventoryConditions);

        var sql = $@"
            SELECT 
                w.Id as WarehouseId,
                w.Name as WarehouseName,
                w.Location,
                COALESCE(SUM(ii.TotalStock), 0) as TotalStock,
                COALESCE(SUM(ii.TotalStock - ii.ReservedStock), 0) as AvailableStock,
                COALESCE(SUM(ii.ReservedStock), 0) as ReservedStock,
                COUNT(ii.Id) as VariantCount,
                COUNT(CASE WHEN ii.TotalStock <= @LowStockThreshold AND ii.TotalStock > 0 THEN 1 END) as LowStockVariantCount,
                COUNT(CASE WHEN ii.TotalStock <= 0 THEN 1 END) as OutOfStockVariantCount,
                COUNT(CASE WHEN ii.ExpiryDate IS NOT NULL AND ii.ExpiryDate <= @CurrentDate THEN 1 END) as ExpiredVariantCount,
                MAX(ii.UpdatedAtUtc) as LastUpdatedUtc
            FROM Warehouses w
            LEFT JOIN InventoryItems ii ON w.Id = ii.WarehouseId AND {inventoryWhereClause}
            WHERE {whereClause}
            GROUP BY w.Id, w.Name, w.Location
            ORDER BY w.Name";

        var result = await connection.QueryAsync<WarehouseStockDto>(sql, parameters);
        return result.ToList();
    }

    public async Task<StockMovementRatesDto> GetStockMovementRatesAsync(
        DateTime fromDate,
        DateTime toDate,
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        List<string>? movementTypes = null,
        string groupBy = "day")
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var whereConditions = new List<string> { "sm.IsDeleted = false", "ii.IsDeleted = false" };
        var parameters = GetTenantParameters(new { FromDate = fromDate, ToDate = toDate });

        whereConditions.Add("sm.TimestampUtc >= @FromDate AND sm.TimestampUtc <= @ToDate");

        if (warehouseIds?.Any() == true)
        {
            whereConditions.Add("ii.WarehouseId = ANY(@WarehouseIds)");
            parameters.Add("WarehouseIds", warehouseIds.ToArray());
        }

        if (variantIds?.Any() == true)
        {
            whereConditions.Add("ii.VariantId = ANY(@VariantIds)");
            parameters.Add("VariantIds", variantIds.ToArray());
        }

        if (movementTypes?.Any() == true)
        {
            whereConditions.Add("sm.Type = ANY(@MovementTypes)");
            parameters.Add("MovementTypes", movementTypes.ToArray());
        }

        var whereClause = string.Join(" AND ", whereConditions);

        var sql = $@"
            -- Movement type rates
            SELECT 
                sm.Type as MovementType,
                COALESCE(SUM(ABS(sm.Quantity)), 0) as TotalQuantity,
                COUNT(sm.Id) as MovementCount,
                COALESCE(AVG(ABS(sm.Quantity)), 0) as AverageQuantity
            FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            WHERE {whereClause}
            GROUP BY sm.Type
            ORDER BY TotalQuantity DESC;

            -- Warehouse rates
            SELECT 
                w.Id as WarehouseId,
                w.Name as WarehouseName,
                COALESCE(SUM(ABS(sm.Quantity)), 0) as TotalQuantity,
                COUNT(sm.Id) as MovementCount,
                COALESCE(AVG(ABS(sm.Quantity)), 0) as AverageQuantity
            FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            INNER JOIN Warehouses w ON ii.WarehouseId = w.Id
            WHERE {whereClause}
            GROUP BY w.Id, w.Name
            ORDER BY TotalQuantity DESC;

            -- Overall totals
            SELECT 
                COALESCE(SUM(ABS(sm.Quantity)), 0) as TotalMovementVolume,
                COUNT(sm.Id) as TotalMovementCount
            FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            WHERE {whereClause};";

        using var multi = await connection.QueryMultipleAsync(sql, parameters);

        var movementTypeRates = await multi.ReadAsync<MovementTypeRateDto>();
        var warehouseRates = await multi.ReadAsync<WarehouseMovementRateDto>();
        var totals = await multi.ReadSingleAsync();

        var totalVolume = (decimal)totals.TotalMovementVolume;
        var movementTypeRatesList = movementTypeRates.ToList();

        // Calculate percentages
        foreach (var rate in movementTypeRatesList)
        {
            rate.PercentageOfTotal = totalVolume > 0 ? (rate.TotalQuantity / totalVolume) * 100 : 0;
        }

        return new StockMovementRatesDto
        {
            PeriodStart = fromDate,
            PeriodEnd = toDate,
            MovementTypeRates = movementTypeRatesList,
            WarehouseRates = warehouseRates.ToList(),
            TotalMovementVolume = totalVolume,
            TotalMovementCount = totals.TotalMovementCount,
            AverageMovementSize = totals.TotalMovementCount > 0 ? totalVolume / totals.TotalMovementCount : 0
        };
    }

    public async Task<PagedResult<AlertDto>> GetActiveAlertsAsync(
        List<string>? alertTypes = null,
        List<string>? severities = null,
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        bool includeAcknowledged = false,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50,
        string sortBy = "CreatedAtUtc",
        string sortDirection = "desc")
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var whereConditions = new List<string> { "a.IsDeleted = false", "a.IsActive = true" };
        var parameters = GetTenantParameters();

        if (!includeAcknowledged)
        {
            whereConditions.Add("a.AcknowledgedAtUtc IS NULL");
        }

        if (alertTypes?.Any() == true)
        {
            whereConditions.Add("a.AlertType = ANY(@AlertTypes)");
            parameters.Add("AlertTypes", alertTypes.ToArray());
        }

        if (severities?.Any() == true)
        {
            whereConditions.Add("a.Severity = ANY(@Severities)");
            parameters.Add("Severities", severities.ToArray());
        }

        if (warehouseIds?.Any() == true)
        {
            whereConditions.Add("a.WarehouseId = ANY(@WarehouseIds)");
            parameters.Add("WarehouseIds", warehouseIds.ToArray());
        }

        if (variantIds?.Any() == true)
        {
            whereConditions.Add("a.VariantId = ANY(@VariantIds)");
            parameters.Add("VariantIds", variantIds.ToArray());
        }

        if (fromDate.HasValue)
        {
            whereConditions.Add("a.CreatedAtUtc >= @FromDate");
            parameters.Add("FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            whereConditions.Add("a.CreatedAtUtc <= @ToDate");
            parameters.Add("ToDate", toDate.Value);
        }

        var whereClause = string.Join(" AND ", whereConditions);
        var orderClause = $"ORDER BY a.{sortBy} {sortDirection.ToUpper()}";
        var offset = (page - 1) * pageSize;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var sql = $@"
            -- Count query
            SELECT COUNT(*) 
            FROM Alerts a
            LEFT JOIN Variants v ON a.VariantId = v.Id
            LEFT JOIN Warehouses w ON a.WarehouseId = w.Id
            WHERE {whereClause};

            -- Data query
            SELECT 
                a.Id,
                a.AlertType,
                a.Severity,
                a.Title,
                a.Message,
                a.VariantId,
                v.SKU as VariantSKU,
                v.Name as VariantName,
                a.WarehouseId,
                w.Name as WarehouseName,
                a.Data,
                a.CreatedAtUtc,
                a.AcknowledgedAtUtc,
                a.AcknowledgedBy,
                a.IsActive
            FROM Alerts a
            LEFT JOIN Variants v ON a.VariantId = v.Id
            LEFT JOIN Warehouses w ON a.WarehouseId = w.Id
            WHERE {whereClause}
            {orderClause}
            LIMIT @PageSize OFFSET @Offset;";

        using var multi = await connection.QueryMultipleAsync(sql, parameters);

        var totalCount = await multi.ReadSingleAsync<int>();
        var alerts = await multi.ReadAsync<AlertDto>();

        return new PagedResult<AlertDto>
        {
            Items = alerts.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<AlertDto> CreateOrUpdateAlertAsync(
        string alertType,
        string severity,
        string title,
        string message,
        Guid? variantId = null,
        Guid? warehouseId = null,
        Dictionary<string, object>? data = null)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var parameters = GetTenantParameters(new
        {
            Id = Guid.NewGuid(),
            AlertType = alertType,
            Severity = severity,
            Title = title,
            Message = message,
            VariantId = variantId,
            WarehouseId = warehouseId,
            Data = data != null ? System.Text.Json.JsonSerializer.Serialize(data) : null,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        });

        var sql = @"
            INSERT INTO Alerts (Id, AlertType, Severity, Title, Message, VariantId, WarehouseId, Data, CreatedAtUtc, IsActive, TenantId, IsDeleted)
            VALUES (@Id, @AlertType, @Severity, @Title, @Message, @VariantId, @WarehouseId, @Data, @CreatedAtUtc, @IsActive, @TenantId, false)
            RETURNING *;";

        var alert = await connection.QuerySingleAsync<AlertDto>(sql, parameters);
        return alert;
    }

    public async Task<bool> AcknowledgeAlertAsync(Guid alertId, Guid acknowledgedBy)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var parameters = GetTenantParameters(new
        {
            AlertId = alertId,
            AcknowledgedBy = acknowledgedBy,
            AcknowledgedAtUtc = DateTime.UtcNow
        });

        var sql = @"
            UPDATE Alerts 
            SET AcknowledgedAtUtc = @AcknowledgedAtUtc, AcknowledgedBy = @AcknowledgedBy
            WHERE Id = @AlertId AND TenantId = @TenantId AND IsDeleted = false";

        var rowsAffected = await connection.ExecuteAsync(sql, parameters);
        return rowsAffected > 0;
    }

    public async Task<bool> DeactivateAlertAsync(Guid alertId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var parameters = GetTenantParameters(new { AlertId = alertId });

        var sql = @"
            UPDATE Alerts 
            SET IsActive = false
            WHERE Id = @AlertId AND TenantId = @TenantId AND IsDeleted = false";

        var rowsAffected = await connection.ExecuteAsync(sql, parameters);
        return rowsAffected > 0;
    }

    public async Task<DashboardMetricsDto> GetCachedMetricsAsync(
        DateTime periodStart,
        DateTime periodEnd,
        List<Guid>? warehouseIds = null)
    {
        // Delegate to real-time metrics for now
        // The caching layer is handled by DashboardCacheService
        return await GetRealTimeMetricsAsync(warehouseIds, null, periodStart, periodEnd);
    }

    public async Task UpdateCachedMetricsAsync(
        DateTime periodStart,
        DateTime periodEnd,
        DashboardMetricsDto metrics)
    {
        // Cache updates are handled by DashboardCacheService
        // This method is kept for interface compatibility
        await Task.CompletedTask;
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