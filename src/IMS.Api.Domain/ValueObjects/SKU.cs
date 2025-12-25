using IMS.Api.Domain.Common;
using System.Text.RegularExpressions;

namespace IMS.Api.Domain.ValueObjects;

public sealed class SKU : ValueObject
{
    private static readonly Regex SkuPattern = new(@"^[A-Z0-9\-_]{3,50}$", RegexOptions.Compiled);
    
    public string Value { get; }

    private SKU(string value)
    {
        Value = value;
    }

    public static SKU Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SKU cannot be null or empty", nameof(value));

        var normalizedValue = value.Trim().ToUpperInvariant();

        if (!SkuPattern.IsMatch(normalizedValue))
            throw new ArgumentException(
                "SKU must be 3-50 characters long and contain only uppercase letters, numbers, hyphens, and underscores", 
                nameof(value));

        return new SKU(normalizedValue);
    }

    public static implicit operator string(SKU sku)
    {
        return sku.Value;
    }

    public static explicit operator SKU(string value)
    {
        return Create(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
}