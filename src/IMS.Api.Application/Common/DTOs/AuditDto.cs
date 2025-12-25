using IMS.Api.Domain.Enums;

namespace IMS.Api.Application.Common.DTOs;

/// <summary>
/// Data transfer object for audit log entries
/// </summary>
public class AuditLogDto
{
    /// <summary>
    /// Gets or sets the audit log ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the action that was performed
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type that was affected
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity ID that was affected
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the user who performed the action
    /// </summary>
    public Guid ActorId { get; set; }

    /// <summary>
    /// Gets or sets the actor's display name
    /// </summary>
    public string? ActorName { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the action was performed
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// Gets or sets the description of the action
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the old values (JSON)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// Gets or sets the new values (JSON)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the action was performed
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets additional context data (JSON)
    /// </summary>
    public string? AdditionalData { get; set; }

    /// <summary>
    /// Gets or sets the warehouse ID if applicable
    /// </summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the warehouse name if applicable
    /// </summary>
    public string? WarehouseName { get; set; }

    /// <summary>
    /// Gets or sets the variant ID if applicable
    /// </summary>
    public Guid? VariantId { get; set; }

    /// <summary>
    /// Gets or sets the variant name if applicable
    /// </summary>
    public string? VariantName { get; set; }

    /// <summary>
    /// Gets or sets the SKU if applicable
    /// </summary>
    public string? SKU { get; set; }

    /// <summary>
    /// Gets or sets the reason for the action
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Data transfer object for audit log summary information
/// </summary>
public class AuditSummaryDto
{
    /// <summary>
    /// Gets or sets the total number of audit entries
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Gets or sets the number of entries in the last 24 hours
    /// </summary>
    public int EntriesLast24Hours { get; set; }

    /// <summary>
    /// Gets or sets the number of entries in the last 7 days
    /// </summary>
    public int EntriesLast7Days { get; set; }

    /// <summary>
    /// Gets or sets the number of entries in the last 30 days
    /// </summary>
    public int EntriesLast30Days { get; set; }

    /// <summary>
    /// Gets or sets the most active users
    /// </summary>
    public List<UserActivityDto> MostActiveUsers { get; set; } = new();

    /// <summary>
    /// Gets or sets the action breakdown
    /// </summary>
    public List<ActionBreakdownDto> ActionBreakdown { get; set; } = new();

    /// <summary>
    /// Gets or sets the entity type breakdown
    /// </summary>
    public List<EntityTypeBreakdownDto> EntityTypeBreakdown { get; set; } = new();
}

/// <summary>
/// Data transfer object for user activity information
/// </summary>
public class UserActivityDto
{
    /// <summary>
    /// Gets or sets the user ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the user name
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of actions performed
    /// </summary>
    public int ActionCount { get; set; }

    /// <summary>
    /// Gets or sets the last action timestamp
    /// </summary>
    public DateTime LastActionUtc { get; set; }
}

/// <summary>
/// Data transfer object for action breakdown information
/// </summary>
public class ActionBreakdownDto
{
    /// <summary>
    /// Gets or sets the action type
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of this action type
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total actions
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// Data transfer object for entity type breakdown information
/// </summary>
public class EntityTypeBreakdownDto
{
    /// <summary>
    /// Gets or sets the entity type
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of actions on this entity type
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total actions
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// Data transfer object for compliance report
/// </summary>
public class ComplianceReportDto
{
    /// <summary>
    /// Gets or sets the report generation timestamp
    /// </summary>
    public DateTime GeneratedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the report period start
    /// </summary>
    public DateTime PeriodStartUtc { get; set; }

    /// <summary>
    /// Gets or sets the report period end
    /// </summary>
    public DateTime PeriodEndUtc { get; set; }

    /// <summary>
    /// Gets or sets the total number of audit entries in the period
    /// </summary>
    public int TotalAuditEntries { get; set; }

    /// <summary>
    /// Gets or sets the number of stock movements in the period
    /// </summary>
    public int StockMovements { get; set; }

    /// <summary>
    /// Gets or sets the number of user actions in the period
    /// </summary>
    public int UserActions { get; set; }

    /// <summary>
    /// Gets or sets the number of system changes in the period
    /// </summary>
    public int SystemChanges { get; set; }

    /// <summary>
    /// Gets or sets the number of failed operations in the period
    /// </summary>
    public int FailedOperations { get; set; }

    /// <summary>
    /// Gets or sets the user activity summary
    /// </summary>
    public List<UserActivityDto> UserActivity { get; set; } = new();

    /// <summary>
    /// Gets or sets the warehouse activity summary
    /// </summary>
    public List<WarehouseActivityDto> WarehouseActivity { get; set; } = new();

    /// <summary>
    /// Gets or sets any compliance violations or anomalies detected
    /// </summary>
    public List<ComplianceViolationDto> Violations { get; set; } = new();
}

/// <summary>
/// Data transfer object for warehouse activity information
/// </summary>
public class WarehouseActivityDto
{
    /// <summary>
    /// Gets or sets the warehouse ID
    /// </summary>
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the warehouse name
    /// </summary>
    public string WarehouseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of actions in this warehouse
    /// </summary>
    public int ActionCount { get; set; }

    /// <summary>
    /// Gets or sets the number of stock movements in this warehouse
    /// </summary>
    public int StockMovements { get; set; }

    /// <summary>
    /// Gets or sets the last activity timestamp
    /// </summary>
    public DateTime LastActivityUtc { get; set; }
}

/// <summary>
/// Data transfer object for compliance violation information
/// </summary>
public class ComplianceViolationDto
{
    /// <summary>
    /// Gets or sets the violation type
    /// </summary>
    public string ViolationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the violation description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity level
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the violation was detected
    /// </summary>
    public DateTime DetectedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the entity ID related to the violation
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the user ID related to the violation
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets additional details about the violation
    /// </summary>
    public string? Details { get; set; }
}