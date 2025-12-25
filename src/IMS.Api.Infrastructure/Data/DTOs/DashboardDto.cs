namespace IMS.Api.Infrastructure.Data.DTOs;

/// <summary>
/// DTO for dashboard metrics and real-time data
/// </summary>
public class DashboardMetricsDto
{
    public decimal TotalStockValue { get; set; }
    public decimal TotalAvailableStock { get; set; }
    public decimal TotalReservedStock { get; set; }
    public int LowStockVariantCount { get; set; }
    public int OutOfStockVariantCount { get; set; }
    public int ExpiredItemCount { get; set; }
    public int NearExpiryItemCount { get; set; }
    public List<WarehouseStockDto> WarehouseBreakdown { get; set; } = new();
    public StockMovementRatesDto MovementRates { get; set; } = new();
    public DateTime GeneratedAtUtc { get; set; }
}

/// <summary>
/// DTO for warehouse-specific stock information
/// </summary>
public class WarehouseStockDto
{
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal TotalStock { get; set; }
    public decimal AvailableStock { get; set; }
    public decimal ReservedStock { get; set; }
    public int VariantCount { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int ExpiredItemCount { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}

/// <summary>
/// DTO for stock movement rates and trends
/// </summary>
public class StockMovementRatesDto
{
    public decimal DailyInboundRate { get; set; }
    public decimal DailyOutboundRate { get; set; }
    public decimal WeeklyInboundRate { get; set; }
    public decimal WeeklyOutboundRate { get; set; }
    public decimal MonthlyInboundRate { get; set; }
    public decimal MonthlyOutboundRate { get; set; }
    public List<MovementTrendDto> DailyTrends { get; set; } = new();
    public DateTime CalculatedAtUtc { get; set; }
}

/// <summary>
/// DTO for movement trend data points
/// </summary>
public class MovementTrendDto
{
    public DateTime Date { get; set; }
    public decimal InboundQuantity { get; set; }
    public decimal OutboundQuantity { get; set; }
    public decimal NetMovement { get; set; }
    public int TransactionCount { get; set; }
}

/// <summary>
/// DTO for low stock alerts
/// </summary>
public class LowStockVariantDto
{
    public Guid VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReservedStock { get; set; }
    public decimal AvailableStock { get; set; }
    public decimal LowStockThreshold { get; set; }
    public string BaseUnit { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public bool IsExpired { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public DateTime LastMovementUtc { get; set; }
}

/// <summary>
/// DTO for real-time events and notifications
/// </summary>
public class RealTimeEventDto
{
    public string EventType { get; set; } = string.Empty;
    public Guid? VariantId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime TimestampUtc { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Info, Warning, Error, Critical
}

/// <summary>
/// DTO for alert notifications
/// </summary>
public class AlertDto
{
    public Guid Id { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public Guid? VariantId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public Guid? AcknowledgedBy { get; set; }
    public bool IsActive { get; set; }
}