using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class PricingTierId : ValueObject
{
    public Guid Value { get; }

    private PricingTierId(Guid value)
    {
        Value = value;
    }

    public static PricingTierId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PricingTierId cannot be empty", nameof(value));

        return new PricingTierId(value);
    }

    public static PricingTierId CreateNew()
    {
        return new PricingTierId(Guid.NewGuid());
    }

    public static implicit operator Guid(PricingTierId pricingTierId)
    {
        return pricingTierId.Value;
    }

    public static explicit operator PricingTierId(Guid value)
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