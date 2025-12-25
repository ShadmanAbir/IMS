using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class InventoryItemId : ValueObject
{
    public Guid Value { get; }

    private InventoryItemId(Guid value)
    {
        Value = value;
    }

    public static InventoryItemId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("InventoryItemId cannot be empty", nameof(value));

        return new InventoryItemId(value);
    }

    public static InventoryItemId CreateNew()
    {
        return new InventoryItemId(Guid.NewGuid());
    }

    public static implicit operator Guid(InventoryItemId inventoryItemId)
    {
        return inventoryItemId.Value;
    }

    public static explicit operator InventoryItemId(Guid value)
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