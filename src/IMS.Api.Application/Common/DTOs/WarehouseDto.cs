namespace IMS.Api.Application.Common.DTOs;

/// <summary>
/// DTO for warehouse entity
/// </summary>
public class WarehouseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsActive { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }
}

/// <summary>
/// DTO for category entity
/// </summary>
public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public int Level { get; set; }
    public string Path { get; set; } = string.Empty; // Hierarchical path like "/electronics/computers/laptops"
    public bool IsActive { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Navigation properties
    public List<CategoryDto> SubCategories { get; set; } = new();
    public int ProductCount { get; set; }
}

/// <summary>
/// DTO for reservation entity
/// </summary>
public class ReservationDto
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string Status { get; set; } = string.Empty; // Active, Expired, Used, Cancelled
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public Guid? UsedBy { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public Guid? CancelledBy { get; set; }
    public Guid TenantId { get; set; }
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Computed properties
    public bool IsExpired => ExpiresAtUtc <= DateTime.UtcNow;
    public bool IsActive => Status == "Active" && !IsExpired;
    public TimeSpan TimeUntilExpiry => ExpiresAtUtc - DateTime.UtcNow;
}

/// <summary>
/// DTO for pricing information
/// </summary>
public class PricingDto
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public string PricingTier { get; set; } = string.Empty; // Standard, Premium, Wholesale, etc.
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal? MinimumQuantity { get; set; }
    public decimal? MaximumQuantity { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public bool IsActive { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Computed properties
    public bool IsCurrentlyEffective => 
        EffectiveFromUtc <= DateTime.UtcNow && 
        (!EffectiveToUtc.HasValue || EffectiveToUtc.Value > DateTime.UtcNow);
}