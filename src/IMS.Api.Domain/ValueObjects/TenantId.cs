using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class TenantId : ValueObject
{
    public Guid Value { get; }

    private TenantId(Guid value)
    {
        Value = value;
    }

    public static TenantId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty", nameof(value));

        return new TenantId(value);
    }

    public static TenantId CreateNew()
    {
        return new TenantId(Guid.NewGuid());
    }

    public static implicit operator Guid(TenantId tenantId)
    {
        return tenantId.Value;
    }

    public static explicit operator TenantId(Guid value)
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