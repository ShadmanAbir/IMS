using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Domain.Enums;
using MediatR;

namespace IMS.Api.Application.Queries.Audit;

/// <summary>
/// Query to generate a compliance report
/// </summary>
public class GetComplianceReportQuery : IRequest<ComplianceReportDto>
{
    /// <summary>
    /// Gets or sets the start date for the report period
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for the report period
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets whether to include detailed violation analysis
    /// </summary>
    public bool IncludeViolationAnalysis { get; set; } = true;

    public GetComplianceReportQuery(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }
}

/// <summary>
/// Handler for GetComplianceReportQuery
/// </summary>
public class GetComplianceReportQueryHandler : IRequestHandler<GetComplianceReportQuery, ComplianceReportDto>
{
    private readonly IAuditRepository _auditRepository;

    public GetComplianceReportQueryHandler(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<ComplianceReportDto> Handle(GetComplianceReportQuery request, CancellationToken cancellationToken)
    {
        var filter = new AuditLogFilter
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PageSize = 10000, // Large page size to get all data for analysis
            PageNumber = 1
        };

        var auditLogs = await _auditRepository.GetAuditLogsAsync(filter, cancellationToken);

        var report = new ComplianceReportDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            PeriodStartUtc = request.StartDate,
            PeriodEndUtc = request.EndDate,
            TotalAuditEntries = auditLogs.TotalCount
        };

        // Analyze audit entries
        var entries = auditLogs.Items.ToList();

        // Count different types of actions
        report.StockMovements = entries.Count(e => e.Action == AuditAction.StockMovement);
        report.UserActions = entries.Count(e => 
            e.Action == AuditAction.Login || 
            e.Action == AuditAction.Logout ||
            e.Action == AuditAction.PermissionGrant ||
            e.Action == AuditAction.PermissionRevoke);
        report.SystemChanges = entries.Count(e => 
            e.Action == AuditAction.Create ||
            e.Action == AuditAction.Update ||
            e.Action == AuditAction.Delete ||
            e.Action == AuditAction.ConfigurationChange);

        // Count failed operations (this would need to be enhanced based on actual error tracking)
        report.FailedOperations = entries.Count(e => 
            e.Description.ToLowerInvariant().Contains("failed") ||
            e.Description.ToLowerInvariant().Contains("error"));

        // Calculate user activity
        var userActivity = entries
            .GroupBy(e => e.ActorId)
            .Select(g => new UserActivityDto
            {
                UserId = g.Key.Value,
                ActionCount = g.Count(),
                LastActionUtc = g.Max(e => e.TimestampUtc)
            })
            .OrderByDescending(u => u.ActionCount)
            .Take(20)
            .ToList();

        report.UserActivity = userActivity;

        // Calculate warehouse activity
        var warehouseActivity = entries
            .Where(e => e.WarehouseId != null)
            .GroupBy(e => e.WarehouseId!)
            .Select(g => new WarehouseActivityDto
            {
                WarehouseId = g.Key.Value,
                ActionCount = g.Count(),
                StockMovements = g.Count(e => e.Action == AuditAction.StockMovement),
                LastActivityUtc = g.Max(e => e.TimestampUtc)
            })
            .OrderByDescending(w => w.ActionCount)
            .ToList();

        report.WarehouseActivity = warehouseActivity;

        // Analyze for compliance violations if requested
        if (request.IncludeViolationAnalysis)
        {
            report.Violations = AnalyzeComplianceViolations(entries);
        }

        return report;
    }

    private List<ComplianceViolationDto> AnalyzeComplianceViolations(List<Domain.Entities.AuditLog> entries)
    {
        var violations = new List<ComplianceViolationDto>();

        // Check for unusual activity patterns
        violations.AddRange(DetectUnusualActivityPatterns(entries));

        // Check for unauthorized access attempts
        violations.AddRange(DetectUnauthorizedAccess(entries));

        // Check for data integrity issues
        violations.AddRange(DetectDataIntegrityIssues(entries));

        // Check for suspicious stock movements
        violations.AddRange(DetectSuspiciousStockMovements(entries));

        return violations.OrderByDescending(v => v.DetectedAtUtc).ToList();
    }

    private List<ComplianceViolationDto> DetectUnusualActivityPatterns(List<Domain.Entities.AuditLog> entries)
    {
        var violations = new List<ComplianceViolationDto>();

        // Detect users with unusually high activity
        var userActivity = entries
            .GroupBy(e => e.ActorId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToList();

        if (userActivity.Any())
        {
            var averageActivity = userActivity.Average(u => u.Count);
            var threshold = averageActivity * 3; // 3x average is considered unusual

            var unusualUsers = userActivity.Where(u => u.Count > threshold).ToList();

            foreach (var user in unusualUsers)
            {
                violations.Add(new ComplianceViolationDto
                {
                    ViolationType = "UnusualActivity",
                    Description = $"User performed {user.Count} actions, which is {user.Count / averageActivity:F1}x the average",
                    Severity = "Medium",
                    DetectedAtUtc = DateTime.UtcNow,
                    UserId = user.UserId.Value,
                    Details = $"Average activity: {averageActivity:F1}, User activity: {user.Count}"
                });
            }
        }

        return violations;
    }

    private List<ComplianceViolationDto> DetectUnauthorizedAccess(List<Domain.Entities.AuditLog> entries)
    {
        var violations = new List<ComplianceViolationDto>();

        // Detect failed login attempts (this would need actual failed login tracking)
        var failedActions = entries
            .Where(e => e.Description.ToLowerInvariant().Contains("failed") ||
                       e.Description.ToLowerInvariant().Contains("unauthorized"))
            .ToList();

        foreach (var failedAction in failedActions)
        {
            violations.Add(new ComplianceViolationDto
            {
                ViolationType = "UnauthorizedAccess",
                Description = failedAction.Description,
                Severity = "High",
                DetectedAtUtc = failedAction.TimestampUtc,
                EntityId = failedAction.EntityId,
                UserId = failedAction.ActorId.Value,
                Details = $"IP: {failedAction.Context.IpAddress}, UserAgent: {failedAction.Context.UserAgent}"
            });
        }

        return violations;
    }

    private List<ComplianceViolationDto> DetectDataIntegrityIssues(List<Domain.Entities.AuditLog> entries)
    {
        var violations = new List<ComplianceViolationDto>();

        // Detect rapid successive changes to the same entity
        var entityChanges = entries
            .Where(e => !string.IsNullOrEmpty(e.EntityId))
            .GroupBy(e => new { e.EntityType, e.EntityId })
            .Where(g => g.Count() > 5) // More than 5 changes to same entity
            .ToList();

        foreach (var entityGroup in entityChanges)
        {
            var changes = entityGroup.OrderBy(e => e.TimestampUtc).ToList();
            var timeSpan = changes.Last().TimestampUtc - changes.First().TimestampUtc;

            if (timeSpan.TotalMinutes < 5) // All changes within 5 minutes
            {
                violations.Add(new ComplianceViolationDto
                {
                    ViolationType = "DataIntegrity",
                    Description = $"Rapid successive changes to {entityGroup.Key.EntityType} {entityGroup.Key.EntityId}",
                    Severity = "Medium",
                    DetectedAtUtc = changes.Last().TimestampUtc,
                    EntityId = entityGroup.Key.EntityId,
                    Details = $"{changes.Count} changes in {timeSpan.TotalMinutes:F1} minutes"
                });
            }
        }

        return violations;
    }

    private List<ComplianceViolationDto> DetectSuspiciousStockMovements(List<Domain.Entities.AuditLog> entries)
    {
        var violations = new List<ComplianceViolationDto>();

        var stockMovements = entries
            .Where(e => e.Action == AuditAction.StockMovement)
            .ToList();

        // Detect large stock adjustments without proper reason
        var suspiciousAdjustments = stockMovements
            .Where(e => e.Description.ToLowerInvariant().Contains("adjustment") &&
                       (string.IsNullOrEmpty(e.Reason) || e.Reason.Length < 10))
            .ToList();

        foreach (var adjustment in suspiciousAdjustments)
        {
            violations.Add(new ComplianceViolationDto
            {
                ViolationType = "SuspiciousStockMovement",
                Description = "Stock adjustment without adequate justification",
                Severity = "High",
                DetectedAtUtc = adjustment.TimestampUtc,
                EntityId = adjustment.EntityId,
                UserId = adjustment.ActorId.Value,
                Details = $"Reason provided: '{adjustment.Reason ?? "None"}'"
            });
        }

        return violations;
    }
}