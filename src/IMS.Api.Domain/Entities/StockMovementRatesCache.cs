using IMS.Api.Domain.Common;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// Materialized view entity for cached stock movement rates
/// Stores pre-calculated movement rate data for improved performance
/// </summary>
public class StockMovementRatesCache : Entity<Guid>
{
    public TenantId TenantId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public Guid? VariantId { get; private set; }
    public string MovementType { get; private set; } = string.Empty;
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public string PeriodType { get; private set; } = string.Empty; // "hour", "day", "week", "month"
    
    // Rate metrics
    public decimal TotalQuantity { get; private set; }
    public int MovementCount { get; private set; }
    public decimal AverageQuantity { get; private set; }
    public decimal PercentageOfTotal { get; private set; }
    
    // Metadata
    public DateTime CalculatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsStale { get; private set; }

    private StockMovementRatesCache() : base(Guid.NewGuid()) { } // EF Core constructor

    public StockMovementRatesCache(
        TenantId tenantId,
        Guid? warehouseId,
        Guid? variantId,
        string movementType,
        DateTime periodStart,
        DateTime periodEnd,
        string periodType,
        decimal totalQuantity,
        int movementCount,
        decimal averageQuantity,
        decimal percentageOfTotal,
        DateTime expiresAtUtc) : base(Guid.NewGuid())
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        WarehouseId = warehouseId;
        VariantId = variantId;
        MovementType = movementType ?? throw new ArgumentNullException(nameof(movementType));
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        PeriodType = periodType ?? throw new ArgumentNullException(nameof(periodType));
        TotalQuantity = totalQuantity;
        MovementCount = movementCount;
        AverageQuantity = averageQuantity;
        PercentageOfTotal = percentageOfTotal;
        CalculatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
        IsStale = false;
    }

    public void UpdateRates(
        decimal totalQuantity,
        int movementCount,
        decimal averageQuantity,
        decimal percentageOfTotal,
        DateTime expiresAtUtc)
    {
        TotalQuantity = totalQuantity;
        MovementCount = movementCount;
        AverageQuantity = averageQuantity;
        PercentageOfTotal = percentageOfTotal;
        CalculatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
        IsStale = false;
    }

    public void MarkAsStale()
    {
        IsStale = true;
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc || IsStale;

    public static string GetCacheKey(TenantId tenantId, Guid? warehouseId, Guid? variantId, string movementType, DateTime periodStart, DateTime periodEnd, string periodType)
    {
        var warehouseKey = warehouseId?.ToString() ?? "all";
        var variantKey = variantId?.ToString() ?? "all";
        return $"movement_rates_{tenantId.Value}_{warehouseKey}_{variantKey}_{movementType}_{periodType}_{periodStart:yyyyMMddHH}_{periodEnd:yyyyMMddHH}";
    }
}