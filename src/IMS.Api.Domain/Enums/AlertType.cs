namespace IMS.Api.Domain.Enums;

/// <summary>
/// Enumeration of alert types for operational notifications
/// </summary>
public enum AlertType
{
    LowStock,
    OutOfStock,
    Expired,
    Expiring,
    UnusualAdjustment,
    ReservationExpiry,
    SystemError
}

/// <summary>
/// Enumeration of alert severity levels
/// </summary>
public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}