namespace IMS.Api.Application.Common.DTOs;

public class InventoryItemDto
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal TotalStock { get; set; }
    public decimal ReservedStock { get; set; }
    public bool AllowNegativeStock { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Computed properties
    public decimal AvailableStock => TotalStock - ReservedStock;
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow;
    public bool IsPerishable => ExpiryDate.HasValue;
    public int? DaysUntilExpiry => ExpiryDate.HasValue ? (int?)(ExpiryDate.Value - DateTime.UtcNow).TotalDays : null;
}

public class StockMovementDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal RunningBalance { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Metadata { get; set; } = string.Empty;
    public string EntryType { get; set; } = string.Empty;
    public Guid? PairedMovementId { get; set; }
}

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