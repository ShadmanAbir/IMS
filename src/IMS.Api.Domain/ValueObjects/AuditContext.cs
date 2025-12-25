using IMS.Api.Domain.Common;
using System.Text.Json;

namespace IMS.Api.Domain.ValueObjects;

/// <summary>
/// Value object containing contextual information for audit entries
/// </summary>
public class AuditContext : ValueObject
{
    /// <summary>
    /// Gets the IP address from which the action was performed
    /// </summary>
    public string IpAddress { get; private set; }

    /// <summary>
    /// Gets the user agent string of the client
    /// </summary>
    public string UserAgent { get; private set; }

    /// <summary>
    /// Gets the correlation ID for tracking related operations
    /// </summary>
    public string CorrelationId { get; private set; }

    /// <summary>
    /// Gets additional metadata as a JSON string
    /// </summary>
    public string AdditionalData { get; private set; }

    private AuditContext(string ipAddress, string userAgent, string correlationId, string additionalData)
    {
        IpAddress = ipAddress ?? string.Empty;
        UserAgent = userAgent ?? string.Empty;
        CorrelationId = correlationId ?? string.Empty;
        AdditionalData = additionalData ?? "{}";
    }

    /// <summary>
    /// Creates a new AuditContext with the specified values
    /// </summary>
    /// <param name="ipAddress">The IP address</param>
    /// <param name="userAgent">The user agent string</param>
    /// <param name="correlationId">The correlation ID</param>
    /// <param name="additionalData">Additional metadata as dictionary</param>
    /// <returns>A new AuditContext instance</returns>
    public static AuditContext Create(
        string ipAddress = null,
        string userAgent = null,
        string correlationId = null,
        Dictionary<string, object> additionalData = null)
    {
        var additionalDataJson = additionalData != null && additionalData.Any()
            ? JsonSerializer.Serialize(additionalData)
            : "{}";

        return new AuditContext(
            ipAddress?.Trim(),
            userAgent?.Trim(),
            correlationId?.Trim(),
            additionalDataJson);
    }

    /// <summary>
    /// Creates an empty AuditContext
    /// </summary>
    /// <returns>An empty AuditContext instance</returns>
    public static AuditContext Empty()
    {
        return new AuditContext(string.Empty, string.Empty, string.Empty, "{}");
    }

    /// <summary>
    /// Gets the additional data as a dictionary
    /// </summary>
    /// <returns>Dictionary containing additional data</returns>
    public Dictionary<string, object> GetAdditionalDataAsDictionary()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(AdditionalData) 
                   ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Creates a new AuditContext with updated additional data
    /// </summary>
    /// <param name="additionalData">New additional data</param>
    /// <returns>A new AuditContext instance</returns>
    public AuditContext WithAdditionalData(Dictionary<string, object> additionalData)
    {
        return Create(IpAddress, UserAgent, CorrelationId, additionalData);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return IpAddress;
        yield return UserAgent;
        yield return CorrelationId;
        yield return AdditionalData;
    }
}