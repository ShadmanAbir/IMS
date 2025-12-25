using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class UnitConversion : ValueObject
{
    public Guid Id { get; }
    public UnitOfMeasure FromUnit { get; }
    public UnitOfMeasure ToUnit { get; }
    public decimal ConversionFactor { get; }
    public DateTime CreatedAtUtc { get; }

    // Parameterless constructor for EF Core
    private UnitConversion()
    {
        Id = Guid.NewGuid();
        FromUnit = UnitOfMeasure.Gram;
        ToUnit = UnitOfMeasure.Gram;
        ConversionFactor = 1;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private UnitConversion(UnitOfMeasure fromUnit, UnitOfMeasure toUnit, decimal conversionFactor)
    {
        Id = Guid.NewGuid();
        FromUnit = fromUnit;
        ToUnit = toUnit;
        ConversionFactor = conversionFactor;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static UnitConversion Create(UnitOfMeasure fromUnit, UnitOfMeasure toUnit, decimal conversionFactor)
    {
        if (fromUnit == null)
            throw new ArgumentNullException(nameof(fromUnit));

        if (toUnit == null)
            throw new ArgumentNullException(nameof(toUnit));

        if (fromUnit.Type != toUnit.Type)
            throw new ArgumentException("Cannot convert between different unit types");

        if (conversionFactor <= 0)
            throw new ArgumentException("Conversion factor must be positive", nameof(conversionFactor));

        return new UnitConversion(fromUnit, toUnit, conversionFactor);
    }

    public decimal Convert(decimal value)
    {
        return value * ConversionFactor;
    }

    public decimal ConvertBack(decimal value)
    {
        return value / ConversionFactor;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FromUnit;
        yield return ToUnit;
        yield return ConversionFactor;
    }

    public override string ToString()
    {
        return $"{FromUnit} -> {ToUnit} (x{ConversionFactor})";
    }
}