using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class StockMovementId : ValueObject
{
    public Guid Value { get; }

    private StockMovementId(Guid value)
    {
        Value = value;
    }

    public static StockMovementId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("StockMovementId cannot be empty", nameof(value));

        return new StockMovementId(value);
    }

    public static StockMovementId CreateNew()
    {
        return new StockMovementId(Guid.NewGuid());
    }

    public static implicit operator Guid(StockMovementId stockMovementId)
    {
        return stockMovementId.Value;
    }

    public static explicit operator StockMovementId(Guid value)
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