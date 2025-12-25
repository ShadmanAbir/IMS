using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class ProductId : ValueObject
{
    public Guid Value { get; }

    private ProductId(Guid value)
    {
        Value = value;
    }

    public static ProductId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty", nameof(value));

        return new ProductId(value);
    }

    public static ProductId CreateNew()
    {
        return new ProductId(Guid.NewGuid());
    }

    public static implicit operator Guid(ProductId productId)
    {
        return productId.Value;
    }

    public static explicit operator ProductId(Guid value)
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