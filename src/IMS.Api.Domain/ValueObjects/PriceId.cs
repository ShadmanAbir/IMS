using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class PriceId : ValueObject
{
    public Guid Value { get; }

    private PriceId(Guid value)
    {
        Value = value;
    }

    public static PriceId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PriceId cannot be empty", nameof(value));

        return new PriceId(value);
    }

    public static PriceId CreateNew()
    {
        return new PriceId(Guid.NewGuid());
    }

    public static implicit operator Guid(PriceId priceId)
    {
        return priceId.Value;
    }

    public static explicit operator PriceId(Guid value)
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