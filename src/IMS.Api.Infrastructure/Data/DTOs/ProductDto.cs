using System.ComponentModel.DataAnnotations;

namespace IMS.Api.Infrastructure.Data.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;
    
    public Guid? CategoryId { get; set; }
    
    [Required]
    public Guid TenantId { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }
}

public class ProductAttributeDto
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Value { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string DataType { get; set; } = string.Empty;
    
    [Required]
    public Guid ProductId { get; set; }
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }
}

public class VariantDto
{
    public Guid Id { get; set; }
    
    [Required]
    [SKUFormat]
    public string Sku { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(10)]
    public string BaseUnitSymbol { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string BaseUnitName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string BaseUnitType { get; set; } = string.Empty;
    
    [Required]
    public Guid ProductId { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }
}

public class VariantAttributeDto
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Value { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string DataType { get; set; } = string.Empty;
    
    [Required]
    public Guid VariantId { get; set; }
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }
}

public class UnitConversionDto
{
    public int Id { get; set; }
    public string FromUnitSymbol { get; set; } = string.Empty;
    public string FromUnitName { get; set; } = string.Empty;
    public string FromUnitType { get; set; } = string.Empty;
    public string ToUnitSymbol { get; set; } = string.Empty;
    public string ToUnitName { get; set; } = string.Empty;
    public string ToUnitType { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; }
    public Guid VariantId { get; set; }
}