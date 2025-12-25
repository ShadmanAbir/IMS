namespace IMS.Api.Application.Common.DTOs;

/// <summary>
/// DTO for real-time dashboard metrics overview
/// </summary>
public class DashboardMetricsDto
{
    public decimal TotalStockValue { get; set; }
    public decimal TotalAvailableStock { get; set; }
    public decimal TotalReservedStock { get; set; }
    public int LowStockVariantCount { get; set; }
    public int OutOfStockVariantCount { get; set; }
    public int ExpiredVariantCount { get; set; }
    public int ExpiringVariantCount { get; set; }
    public List<WarehouseStockDto> WarehouseBreakdown { get; set; } = new();
    public StockMovementRatesDto MovementRates { get; set; } = new();
    public DateTime GeneratedAtUtc { get; set; }
}

/// <summary>
/// DTO for warehouse-specific stock levels
/// </summary>
public class WarehouseStockDto
{
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal TotalStock { get; set; }
    public decimal AvailableStock { get; set; }
    public decimal ReservedStock { get; set; }
    public int VariantCount { get; set; }
    public int LowStockVariantCount { get; set; }
    public int OutOfStockVariantCount { get; set; }
    public int ExpiredVariantCount { get; set; }
    public DateTime LastUpdatedUtc { get; set; }

    // Per-variant breakdown for the warehouse (added for tests)
    public List<VariantStockDto> VariantStockLevels { get; set; } = new();
}

public class VariantStockDto
{
    public Guid VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public decimal TotalStock { get; set; }
    public decimal AvailableStock { get; set; }
    public decimal ReservedStock { get; set; }
    public Guid ProductId { get; set; }
}

/// <summary>
/// DTO for stock movement rates and trends
/// </summary>
public class StockMovementRatesDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<MovementTypeRateDto> MovementTypeRates { get; set; } = new();
    public List<WarehouseMovementRateDto> WarehouseRates { get; set; } = new();
    public decimal TotalMovementVolume { get; set; }
    public int TotalMovementCount { get; set; }
    public decimal AverageMovementSize { get; set; }
}

/// <summary>
/// DTO for movement rates by type
/// </summary>
public class MovementTypeRateDto
{
    public string MovementType { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public int MovementCount { get; set; }
    public decimal AverageQuantity { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

/// <summary>
/// DTO for warehouse-specific movement rates
/// </summary>
public class WarehouseMovementRateDto
{
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public int MovementCount { get; set; }
    public decimal AverageQuantity { get; set; }
}

/// <summary>
/// DTO for operational alerts
/// </summary>
public class AlertDto
{
    public Guid Id { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? VariantId { get; set; }
    public string? VariantSKU { get; set; }
    public string? VariantName { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public Guid? AcknowledgedBy { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for real-time events
/// </summary>
public class RealTimeEventDto
{
    public string EventType { get; set; } = string.Empty;
    public Guid? VariantId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime TimestampUtc { get; set; }
}

/// <summary>
/// DTO for stock level change notifications
/// </summary>
public class StockLevelChangeDto
{
    public Guid VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal PreviousStock { get; set; }
    public decimal NewStock { get; set; }
    public decimal ChangeAmount { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
}

/// <summary>
/// DTO for low stock alert notifications
/// </summary>
public class LowStockAlertDto
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal LowStockThreshold { get; set; }
    public decimal StockDeficit { get; set; }
    public string Severity { get; set; } = string.Empty; // "Low", "Critical", "OutOfStock"
    public DateTime DetectedAtUtc { get; set; }
}

/// <summary>
/// DTO for reservation expiry alert notifications
/// </summary>
public class ReservationExpiryAlertDto
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public Guid VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal ReservedQuantity { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty; // "Expiring", "Expired"
    public DateTime DetectedAtUtc { get; set; }
}

/// <summary>
/// DTO for unusual adjustment pattern alert notifications
/// </summary>
public class UnusualAdjustmentAlertDto
{
    public Guid Id { get; set; }
    public Guid? VariantId { get; set; }
    public string? SKU { get; set; }
    public string? VariantName { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string PatternType { get; set; } = string.Empty; // "FrequencySpike", "LargeAdjustment", "NegativePattern"
    public decimal AdjustmentAmount { get; set; }
    public int AdjustmentCount { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAtUtc { get; set; }
}

/// <summary>
/// DTO for general alert notifications
/// </summary>
public class GeneralAlertDto
{
    public Guid Id { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // "Info", "Warning", "Error", "Critical"
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public bool RequiresAcknowledgment { get; set; }
}