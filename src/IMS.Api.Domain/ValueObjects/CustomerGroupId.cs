using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class CustomerGroupId : ValueObject
{
    public Guid Value { get; }

    private CustomerGroupId(Guid value)
    {
        Value = value;
    }

    public static CustomerGroupId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("CustomerGroupId cannot be empty", nameof(value));

        return new CustomerGroupId(value);
    }

    public static CustomerGroupId CreateNew()
    {
        return new CustomerGroupId(Guid.NewGuid());
    }

    public static implicit operator Guid(CustomerGroupId customerGroupId)
    {
        return customerGroupId.Value;
    }

    public static explicit operator CustomerGroupId(Guid value)
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