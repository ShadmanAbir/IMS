using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class VariantId : ValueObject
{
    public Guid Value { get; }

    private VariantId(Guid value)
    {
        Value = value;
    }

    public static VariantId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("VariantId cannot be empty", nameof(value));

        return new VariantId(value);
    }

    public static VariantId CreateNew()
    {
        return new VariantId(Guid.NewGuid());
    }

    public static implicit operator Guid(VariantId variantId)
    {
        return variantId.Value;
    }

    public static explicit operator VariantId(Guid value)
    {
        return Create(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}