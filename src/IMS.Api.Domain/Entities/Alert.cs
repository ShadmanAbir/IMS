using IMS.Api.Domain.Common;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// Alert entity for operational notifications and dashboard alerts
/// </summary>
public class Alert : SoftDeletableEntity<AlertId>
{
    public AlertType AlertType { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public VariantId? VariantId { get; private set; }
    public WarehouseId? WarehouseId { get; private set; }
    public string Data { get; private set; } = string.Empty; // JSON data
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }
    public UserId? AcknowledgedBy { get; private set; }
    public bool IsActive { get; private set; }
    public TenantId TenantId { get; private set; }

    // EF Core constructor
    private Alert() : base(AlertId.CreateNew()) { }

    private Alert(
        AlertId id,
        AlertType alertType,
        AlertSeverity severity,
        string title,
        string message,
        TenantId tenantId,
        VariantId? variantId = null,
        WarehouseId? warehouseId = null,
        string data = "")
        : base(id)
    {
        AlertType = alertType;
        Severity = severity;
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        VariantId = variantId;
        WarehouseId = warehouseId;
        Data = data ?? string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
        IsActive = true;
    }

    public static Alert Create(
        AlertType alertType,
        AlertSeverity severity,
        string title,
        string message,
        TenantId tenantId,
        VariantId? variantId = null,
        WarehouseId? warehouseId = null,
        string data = "")
    {
        return new Alert(
            AlertId.CreateNew(),
            alertType,
            severity,
            title,
            message,
            tenantId,
            variantId,
            warehouseId,
            data);
    }

    public void Acknowledge(UserId acknowledgedBy)
    {
        if (AcknowledgedAtUtc.HasValue)
            throw new InvalidOperationException("Alert has already been acknowledged");

        AcknowledgedBy = acknowledgedBy ?? throw new ArgumentNullException(nameof(acknowledgedBy));
        AcknowledgedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
        AcknowledgedAtUtc = null;
        AcknowledgedBy = null;
    }

    public bool IsAcknowledged => AcknowledgedAtUtc.HasValue;
    public bool IsExpired(TimeSpan expiryDuration) => CreatedAtUtc.Add(expiryDuration) < DateTime.UtcNow;
}