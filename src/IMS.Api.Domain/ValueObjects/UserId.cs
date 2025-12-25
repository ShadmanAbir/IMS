using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class UserId : ValueObject
{
    public Guid Value { get; }

    private UserId(Guid value)
    {
        Value = value;
    }

    public static UserId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(value));

        return new UserId(value);
    }

    public static UserId CreateNew()
    {
        return new UserId(Guid.NewGuid());
    }

    public static implicit operator Guid(UserId userId)
    {
        return userId.Value;
    }

    public static explicit operator UserId(Guid value)
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