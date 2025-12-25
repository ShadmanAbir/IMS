using IMS.Api.Domain.Common;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// Materialized view entity for cached dashboard metrics
/// Stores pre-calculated dashboard data for improved performance
/// </summary>
public class DashboardMetricsCache : Entity<Guid>
{
    public TenantId TenantId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public string PeriodType { get; private set; } = string.Empty; // "hour", "day", "week", "month"
    
    // Cached metrics
    public decimal TotalStockValue { get; private set; }
    public decimal TotalAvailableStock { get; private set; }
    public decimal TotalReservedStock { get; private set; }
    public int LowStockVariantCount { get; private set; }
    public int OutOfStockVariantCount { get; private set; }
    public int ExpiredVariantCount { get; private set; }
    public int ExpiringVariantCount { get; private set; }
    public int TotalVariantCount { get; private set; }
    
    // Movement metrics
    public decimal TotalMovementVolume { get; private set; }
    public int TotalMovementCount { get; private set; }
    public decimal AverageMovementSize { get; private set; }
    
    // Metadata
    public DateTime CalculatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsStale { get; private set; }
    public string? MetadataJson { get; private set; }

    private DashboardMetricsCache() : base(Guid.NewGuid()) { } // EF Core constructor

    public DashboardMetricsCache(
        TenantId tenantId,
        Guid? warehouseId,
        DateTime periodStart,
        DateTime periodEnd,
        string periodType,
        decimal totalStockValue,
        decimal totalAvailableStock,
        decimal totalReservedStock,
        int lowStockVariantCount,
        int outOfStockVariantCount,
        int expiredVariantCount,
        int expiringVariantCount,
        int totalVariantCount,
        decimal totalMovementVolume,
        int totalMovementCount,
        decimal averageMovementSize,
        DateTime expiresAtUtc,
        string? metadataJson = null) : base(Guid.NewGuid())
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        WarehouseId = warehouseId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        PeriodType = periodType ?? throw new ArgumentNullException(nameof(periodType));
        TotalStockValue = totalStockValue;
        TotalAvailableStock = totalAvailableStock;
        TotalReservedStock = totalReservedStock;
        LowStockVariantCount = lowStockVariantCount;
        OutOfStockVariantCount = outOfStockVariantCount;
        ExpiredVariantCount = expiredVariantCount;
        ExpiringVariantCount = expiringVariantCount;
        TotalVariantCount = totalVariantCount;
        TotalMovementVolume = totalMovementVolume;
        TotalMovementCount = totalMovementCount;
        AverageMovementSize = averageMovementSize;
        CalculatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
        IsStale = false;
        MetadataJson = metadataJson;
    }

    public void UpdateMetrics(
        decimal totalStockValue,
        decimal totalAvailableStock,
        decimal totalReservedStock,
        int lowStockVariantCount,
        int outOfStockVariantCount,
        int expiredVariantCount,
        int expiringVariantCount,
        int totalVariantCount,
        decimal totalMovementVolume,
        int totalMovementCount,
        decimal averageMovementSize,
        DateTime expiresAtUtc,
        string? metadataJson = null)
    {
        TotalStockValue = totalStockValue;
        TotalAvailableStock = totalAvailableStock;
        TotalReservedStock = totalReservedStock;
        LowStockVariantCount = lowStockVariantCount;
        OutOfStockVariantCount = outOfStockVariantCount;
        ExpiredVariantCount = expiredVariantCount;
        ExpiringVariantCount = expiringVariantCount;
        TotalVariantCount = totalVariantCount;
        TotalMovementVolume = totalMovementVolume;
        TotalMovementCount = totalMovementCount;
        AverageMovementSize = averageMovementSize;
        CalculatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
        IsStale = false;
        MetadataJson = metadataJson;
    }

    public void MarkAsStale()
    {
        IsStale = true;
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc || IsStale;

    public static string GetCacheKey(TenantId tenantId, Guid? warehouseId, DateTime periodStart, DateTime periodEnd, string periodType)
    {
        var warehouseKey = warehouseId?.ToString() ?? "all";
        return $"dashboard_metrics_{tenantId.Value}_{warehouseKey}_{periodType}_{periodStart:yyyyMMddHH}_{periodEnd:yyyyMMddHH}";
    }
}