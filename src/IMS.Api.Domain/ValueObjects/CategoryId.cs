using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class CategoryId : ValueObject
{
    public Guid Value { get; }

    private CategoryId(Guid value)
    {
        Value = value;
    }

    public static CategoryId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty", nameof(value));

        return new CategoryId(value);
    }

    public static CategoryId CreateNew()
    {
        return new CategoryId(Guid.NewGuid());
    }

    public static implicit operator Guid(CategoryId categoryId)
    {
        return categoryId.Value;
    }

    public static explicit operator CategoryId(Guid value)
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