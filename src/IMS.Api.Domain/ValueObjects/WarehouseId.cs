using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class WarehouseId : ValueObject
{
    public Guid Value { get; }

    private WarehouseId(Guid value)
    {
        Value = value;
    }

    public static WarehouseId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("WarehouseId cannot be empty", nameof(value));

        return new WarehouseId(value);
    }

    public static WarehouseId CreateNew()
    {
        return new WarehouseId(Guid.NewGuid());
    }

    public static implicit operator Guid(WarehouseId warehouseId)
    {
        return warehouseId.Value;
    }

    public static explicit operator WarehouseId(Guid value)
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