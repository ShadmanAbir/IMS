using System.ComponentModel.DataAnnotations;

namespace IMS.Api.Infrastructure.Data.DTOs;

public class InventoryItemDto
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid VariantId { get; set; }
    
    [Required]
    public Guid WarehouseId { get; set; }
    
    [NonNegativeDecimal]
    public decimal TotalStock { get; set; }
    
    [NonNegativeDecimal]
    public decimal ReservedStock { get; set; }
    
    public bool AllowNegativeStock { get; set; }
    
    [FutureDate]
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
    
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    public decimal Quantity { get; set; }
    
    [Required]
    public decimal RunningBalance { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
    
    [Required]
    public Guid ActorId { get; set; }
    
    public DateTime TimestampUtc { get; set; }
    
    [StringLength(100)]
    public string ReferenceNumber { get; set; } = string.Empty;
    
    public string Metadata { get; set; } = string.Empty;
    
    [Required]
    public Guid InventoryItemId { get; set; }
    
    // Double-entry fields
    [Required]
    [StringLength(20)]
    public string EntryType { get; set; } = string.Empty;
    
    public Guid? PairedMovementId { get; set; }
}

public class InventoryItemWithMovementsDto : InventoryItemDto
{
    public List<StockMovementDto> Movements { get; set; } = new();
}