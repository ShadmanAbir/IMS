using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

public sealed class ReservationId : ValueObject
{
    public Guid Value { get; }

    private ReservationId(Guid value)
    {
        Value = value;
    }

    public static ReservationId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ReservationId cannot be empty", nameof(value));

        return new ReservationId(value);
    }

    public static ReservationId CreateNew()
    {
        return new ReservationId(Guid.NewGuid());
    }

    public static implicit operator Guid(ReservationId reservationId)
    {
        return reservationId.Value;
    }

    public static explicit operator ReservationId(Guid value)
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