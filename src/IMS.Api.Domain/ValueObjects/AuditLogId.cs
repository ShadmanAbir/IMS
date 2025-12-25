using IMS.Api.Domain.Common;

namespace IMS.Api.Domain.ValueObjects;

/// <summary>
/// Value object representing a unique identifier for audit log entries
/// </summary>
public class AuditLogId : ValueObject
{
    public Guid Value { get; private set; }

    private AuditLogId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new AuditLogId with a generated GUID
    /// </summary>
    /// <returns>A new AuditLogId instance</returns>
    public static AuditLogId CreateNew()
    {
        return new AuditLogId(Guid.NewGuid());
    }

    /// <summary>
    /// Creates an AuditLogId from an existing GUID value
    /// </summary>
    /// <param name="value">The GUID value</param>
    /// <returns>An AuditLogId instance</returns>
    public static AuditLogId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("AuditLogId cannot be empty", nameof(value));

        return new AuditLogId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(AuditLogId auditLogId)
    {
        return auditLogId.Value;
    }

    public static implicit operator AuditLogId(Guid value)
    {
        return Create(value);
    }
}