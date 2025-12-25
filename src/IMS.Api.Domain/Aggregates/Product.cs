using IMS.Api.Domain.Common;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Domain.Enums;

namespace IMS.Api.Domain.Aggregates;

/// <summary>
/// Product aggregate root representing a non-sellable container that groups related variants.
/// Products organize inventory into logical groupings and can contain multiple sellable variants.
/// Supports soft delete functionality and cascades delete operations to child variants.
/// </summary>
public class Product : SoftDeletableAggregateRoot<ProductId>
{
    private readonly List<ProductAttribute> _attributes = new();
    private readonly List<Variant> _variants = new();

    /// <summary>
    /// Gets the product name
    /// </summary>
    public string Name { get; private set; }
    
    /// <summary>
    /// Gets the product description
    /// </summary>
    public string Description { get; private set; }
    
    /// <summary>
    /// Gets the category identifier this product belongs to
    /// </summary>
    public CategoryId? CategoryId { get; private set; }
    
    /// <summary>
    /// Gets the tenant identifier for multi-tenant support
    /// </summary>
    public TenantId TenantId { get; private set; }
    
    /// <summary>
    /// Gets the UTC timestamp when the product was created
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }
    
    /// <summary>
    /// Gets the UTC timestamp when the product was last updated
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the read-only collection of product attributes
    /// </summary>
    public IReadOnlyList<ProductAttribute> Attributes => _attributes.AsReadOnly();
    
    /// <summary>
    /// Gets the read-only collection of product variants
    /// </summary>
    public IReadOnlyList<Variant> Variants => _variants.AsReadOnly();

    // Private constructor for domain logic
    private Product(ProductId id, string name, string description, TenantId tenantId, CategoryId? categoryId = null) : base(id)
    {
        Name = name;
        Description = description;
        CategoryId = categoryId;
        TenantId = tenantId;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new product with the specified details
    /// </summary>
    /// <param name="name">The product name (required)</param>
    /// <param name="description">The product description (required)</param>
    /// <param name="tenantId">The tenant identifier (required)</param>
    /// <param name="categoryId">The optional category identifier</param>
    /// <returns>A new Product instance</returns>
    /// <exception cref="ArgumentException">Thrown when name or description is null or empty</exception>
    /// <exception cref="ArgumentNullException">Thrown when tenantId is null</exception>
    public static Product Create(string name, string description, TenantId tenantId, CategoryId? categoryId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Product description cannot be null or empty", nameof(description));

        if (tenantId == null)
            throw new ArgumentNullException(nameof(tenantId));

        return new Product(ProductId.CreateNew(), name.Trim(), description.Trim(), tenantId, categoryId);
    }

    /// <summary>
    /// Updates the product name and description
    /// </summary>
    /// <param name="name">The new product name (required)</param>
    /// <param name="description">The new product description (required)</param>
    /// <exception cref="ArgumentException">Thrown when name or description is null or empty</exception>
    public void UpdateDetails(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Product description cannot be null or empty", nameof(description));

        Name = name.Trim();
        Description = description.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateCategory(CategoryId? categoryId)
    {
        CategoryId = categoryId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddAttribute(string name, string value, AttributeDataType dataType)
    {
        if (_attributes.Any(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Attribute '{name}' already exists");

        var attribute = ProductAttribute.Create(name, value, dataType);
        _attributes.Add(attribute);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateAttribute(string name, string value)
    {
        var attribute = _attributes.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (attribute == null)
            throw new InvalidOperationException($"Attribute '{name}' not found");

        attribute.UpdateValue(value);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RemoveAttribute(string name)
    {
        var attribute = _attributes.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (attribute == null)
            throw new InvalidOperationException($"Attribute '{name}' not found");

        _attributes.Remove(attribute);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Variant AddVariant(SKU sku, string name, UnitOfMeasure baseUnit)
    {
        if (_variants.Any(v => v.Sku == sku))
            throw new InvalidOperationException($"Variant with SKU '{sku}' already exists");

        var variant = Variant.Create(sku, name, baseUnit, Id);
        _variants.Add(variant);
        UpdatedAtUtc = DateTime.UtcNow;

        return variant;
    }

    public void RemoveVariant(VariantId variantId)
    {
        var variant = _variants.FirstOrDefault(v => v.Id == variantId);
        if (variant == null)
            throw new InvalidOperationException($"Variant with ID '{variantId}' not found");

        _variants.Remove(variant);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Variant GetVariant(VariantId variantId)
    {
        var variant = _variants.FirstOrDefault(v => v.Id == variantId);
        if (variant == null)
            throw new InvalidOperationException($"Variant with ID '{variantId}' not found");

        return variant;
    }

    public Variant GetVariantBySku(SKU sku)
    {
        var variant = _variants.FirstOrDefault(v => v.Sku == sku);
        if (variant == null)
            throw new InvalidOperationException($"Variant with SKU '{sku}' not found");

        return variant;
    }

    public bool HasVariant(VariantId variantId)
    {
        return _variants.Any(v => v.Id == variantId);
    }

    /// <summary>
    /// Soft deletes the product and all its variants
    /// </summary>
    /// <param name="deletedBy">The user performing the delete operation</param>
    /// <exception cref="ArgumentNullException">Thrown when deletedBy is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when the product is already deleted</exception>
    public override void SoftDelete(UserId deletedBy)
    {
        // Soft delete all variants when product is deleted
        foreach (var variant in _variants)
        {
            if (variant is ISoftDeletable softDeletableVariant && !softDeletableVariant.IsDeleted)
            {
                softDeletableVariant.SoftDelete(deletedBy);
            }
        }
        
        base.SoftDelete(deletedBy);
    }

    /// <summary>
    /// Restores the product and all its variants from soft delete
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the product is not deleted</exception>
    public override void Restore()
    {
        // Restore all variants when product is restored
        foreach (var variant in _variants)
        {
            if (variant is ISoftDeletable softDeletableVariant && softDeletableVariant.IsDeleted)
            {
                softDeletableVariant.Restore();
            }
        }
        
        base.Restore();
    }
}