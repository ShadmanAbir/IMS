using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

/// <summary>
/// Value object representing an Alert identifier
/// </summary>
public class AlertId : ValueObject
{
    public Guid Value { get; private set; }

    private AlertId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("AlertId cannot be empty", nameof(value));
        
        Value = value;
    }

    public static AlertId Create(Guid value) => new(value);
    public static AlertId CreateNew() => new(Guid.NewGuid());

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(AlertId alertId) => alertId.Value;
    public static implicit operator AlertId(Guid value) => Create(value);
}