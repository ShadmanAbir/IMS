using IMS.Api.Domain.Common;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Domain.Enums;

namespace IMS.Api.Domain.Entities;

public class Variant : SoftDeletableEntity<VariantId>
{
    private readonly List<VariantAttribute> _attributes = new();
    private readonly List<UnitConversion> _unitConversions = new();

    public SKU Sku { get; private set; }
    public string Name { get; private set; }
    public UnitOfMeasure BaseUnit { get; private set; }
    
    // Optional TenantId for multi-tenant scenarios (set via factories or DbContext)
    public TenantId? TenantId { get; private set; }
    public ProductId ProductId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyList<VariantAttribute> Attributes => _attributes.AsReadOnly();
    public IReadOnlyList<UnitConversion> UnitConversions => _unitConversions.AsReadOnly();

    // Private constructor for domain logic
    private Variant(VariantId id, SKU sku, string name, UnitOfMeasure baseUnit, ProductId productId) : base(id)
    {
        Sku = sku;
        Name = name;
        BaseUnit = baseUnit;
        ProductId = productId;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static Variant Create(SKU sku, string name, UnitOfMeasure baseUnit, ProductId productId)
    {
        if (sku == null)
            throw new ArgumentNullException(nameof(sku));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variant name cannot be null or empty", nameof(name));

        if (baseUnit == null)
            throw new ArgumentNullException(nameof(baseUnit));

        if (productId == null)
            throw new ArgumentNullException(nameof(productId));

        return new Variant(VariantId.CreateNew(), sku, name.Trim(), baseUnit, productId);
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variant name cannot be null or empty", nameof(name));

        Name = name.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddAttribute(string name, string value, AttributeDataType dataType)
    {
        if (_attributes.Any(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Attribute '{name}' already exists");

        var attribute = VariantAttribute.Create(name, value, dataType);
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

    public void AddUnitConversion(UnitOfMeasure fromUnit, UnitOfMeasure toUnit, decimal conversionFactor)
    {
        if (fromUnit.Type != BaseUnit.Type && toUnit.Type != BaseUnit.Type)
            throw new ArgumentException("At least one unit must match the base unit type");

        var conversion = UnitConversion.Create(fromUnit, toUnit, conversionFactor);
        
        if (_unitConversions.Any(c => c.FromUnit == fromUnit && c.ToUnit == toUnit))
            throw new InvalidOperationException($"Conversion from {fromUnit} to {toUnit} already exists");

        _unitConversions.Add(conversion);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public decimal ConvertToBaseUnit(decimal quantity, UnitOfMeasure fromUnit)
    {
        if (fromUnit == BaseUnit)
            return quantity;

        var conversion = _unitConversions.FirstOrDefault(c => c.FromUnit == fromUnit && c.ToUnit == BaseUnit);
        if (conversion == null)
            throw new InvalidOperationException($"No conversion found from {fromUnit} to base unit {BaseUnit}");

        return conversion.Convert(quantity);
    }

    public decimal ConvertFromBaseUnit(decimal quantity, UnitOfMeasure toUnit)
    {
        if (toUnit == BaseUnit)
            return quantity;

        var conversion = _unitConversions.FirstOrDefault(c => c.FromUnit == BaseUnit && c.ToUnit == toUnit);
        if (conversion == null)
            throw new InvalidOperationException($"No conversion found from base unit {BaseUnit} to {toUnit}");

        return conversion.Convert(quantity);
    }
}