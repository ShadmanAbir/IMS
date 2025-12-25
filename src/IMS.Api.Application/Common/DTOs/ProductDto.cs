namespace IMS.Api.Application.Common.DTOs;

/// <summary>
/// DTO for Product entity
/// </summary>
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Navigation properties
    public List<ProductAttributeDto> Attributes { get; set; } = new();
    public List<VariantDto> Variants { get; set; } = new();
    public int VariantCount { get; set; }
}

/// <summary>
/// DTO for Variant entity
/// </summary>
public class VariantDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Navigation properties
    public List<VariantAttributeDto> Attributes { get; set; } = new();
    public List<UnitConversionDto> UnitConversions { get; set; } = new();
}

/// <summary>
/// DTO for Product Attribute
/// </summary>
public class ProductAttributeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>
/// DTO for Variant Attribute
/// </summary>
public class VariantAttributeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>
/// DTO for Unit Conversion
/// </summary>
public class UnitConversionDto
{
    public Guid Id { get; set; }
    public string FromUnit { get; set; } = string.Empty;
    public string ToUnit { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// DTO for creating a new product
/// </summary>
public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public List<CreateProductAttributeDto> Attributes { get; set; } = new();
}

/// <summary>
/// DTO for updating a product
/// </summary>
public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
}

/// <summary>
/// DTO for creating a new variant
/// </summary>
public class CreateVariantDto
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public List<CreateVariantAttributeDto> Attributes { get; set; } = new();
    public List<CreateUnitConversionDto> UnitConversions { get; set; } = new();
}

/// <summary>
/// DTO for updating a variant
/// </summary>
public class UpdateVariantDto
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// DTO for creating a product attribute
/// </summary>
public class CreateProductAttributeDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = "String";
}

/// <summary>
/// DTO for creating a variant attribute
/// </summary>
public class CreateVariantAttributeDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = "String";
}

/// <summary>
/// DTO for creating a unit conversion
/// </summary>
public class CreateUnitConversionDto
{
    public string FromUnit { get; set; } = string.Empty;
    public string ToUnit { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; }
}

/// <summary>
/// DTO for updating an attribute
/// </summary>
public class UpdateAttributeDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}