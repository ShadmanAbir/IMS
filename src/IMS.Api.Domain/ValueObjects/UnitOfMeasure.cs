using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class UnitOfMeasure : ValueObject
{
    public string Symbol { get; }
    public string Name { get; }
    public UnitType Type { get; }

    private UnitOfMeasure(string symbol, string name, UnitType type)
    {
        Symbol = symbol;
        Name = name;
        Type = type;
    }

    public static UnitOfMeasure Create(string symbol, string name, UnitType type)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Unit symbol cannot be null or empty", nameof(symbol));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Unit name cannot be null or empty", nameof(name));

        return new UnitOfMeasure(symbol.Trim(), name.Trim(), type);
    }

    // Predefined common units
    public static readonly UnitOfMeasure Gram = Create("g", "Gram", UnitType.Weight);
    public static readonly UnitOfMeasure Kilogram = Create("kg", "Kilogram", UnitType.Weight);
    public static readonly UnitOfMeasure Pound = Create("lb", "Pound", UnitType.Weight);
    public static readonly UnitOfMeasure Ounce = Create("oz", "Ounce", UnitType.Weight);

    public static readonly UnitOfMeasure Milliliter = Create("ml", "Milliliter", UnitType.Volume);
    public static readonly UnitOfMeasure Liter = Create("l", "Liter", UnitType.Volume);
    public static readonly UnitOfMeasure FluidOunce = Create("fl oz", "Fluid Ounce", UnitType.Volume);
    public static readonly UnitOfMeasure Gallon = Create("gal", "Gallon", UnitType.Volume);

    public static readonly UnitOfMeasure Piece = Create("pcs", "Piece", UnitType.Count);
    public static readonly UnitOfMeasure Dozen = Create("dz", "Dozen", UnitType.Count);
    public static readonly UnitOfMeasure Pack = Create("pk", "Pack", UnitType.Count);

    public static readonly UnitOfMeasure Millimeter = Create("mm", "Millimeter", UnitType.Length);
    public static readonly UnitOfMeasure Centimeter = Create("cm", "Centimeter", UnitType.Length);
    public static readonly UnitOfMeasure Meter = Create("m", "Meter", UnitType.Length);
    public static readonly UnitOfMeasure Inch = Create("in", "Inch", UnitType.Length);
    public static readonly UnitOfMeasure Foot = Create("ft", "Foot", UnitType.Length);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Symbol;
        yield return Type;
    }

    public override string ToString()
    {
        return Symbol;
    }
}

public enum UnitType
{
    Weight,
    Volume,
    Count,
    Length
}