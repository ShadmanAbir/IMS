using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using MediatR;

namespace IMS.Api.Application.Queries.Audit;

/// <summary>
/// Query to get audit summary information
/// </summary>
public class GetAuditSummaryQuery : IRequest<AuditSummaryDto>
{
    /// <summary>
    /// Gets or sets the start date for the summary period
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for the summary period
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Handler for GetAuditSummaryQuery
/// </summary>
public class GetAuditSummaryQueryHandler : IRequestHandler<GetAuditSummaryQuery, AuditSummaryDto>
{
    private readonly IAuditRepository _auditRepository;

    public GetAuditSummaryQueryHandler(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<AuditSummaryDto> Handle(GetAuditSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var endDate = request.EndDate ?? now;
        var startDate = request.StartDate ?? now.AddDays(-30);

        // Get total entries
        var totalFilter = new AuditLogFilter
        {
            PageSize = 1,
            PageNumber = 1
        };
        var totalResult = await _auditRepository.GetAuditLogsAsync(totalFilter, cancellationToken);

        // Get entries for different time periods
        var last24HoursFilter = new AuditLogFilter
        {
            StartDate = now.AddDays(-1),
            EndDate = now,
            PageSize = 1,
            PageNumber = 1
        };
        var last24HoursResult = await _auditRepository.GetAuditLogsAsync(last24HoursFilter, cancellationToken);

        var last7DaysFilter = new AuditLogFilter
        {
            StartDate = now.AddDays(-7),
            EndDate = now,
            PageSize = 1,
            PageNumber = 1
        };
        var last7DaysResult = await _auditRepository.GetAuditLogsAsync(last7DaysFilter, cancellationToken);

        var last30DaysFilter = new AuditLogFilter
        {
            StartDate = now.AddDays(-30),
            EndDate = now,
            PageSize = 1,
            PageNumber = 1
        };
        var last30DaysResult = await _auditRepository.GetAuditLogsAsync(last30DaysFilter, cancellationToken);

        // Get detailed data for the specified period
        var periodFilter = new AuditLogFilter
        {
            StartDate = startDate,
            EndDate = endDate,
            PageSize = 10000, // Large page size to get all data for analysis
            PageNumber = 1
        };
        var periodResult = await _auditRepository.GetAuditLogsAsync(periodFilter, cancellationToken);

        var summary = new AuditSummaryDto
        {
            TotalEntries = totalResult.TotalCount,
            EntriesLast24Hours = last24HoursResult.TotalCount,
            EntriesLast7Days = last7DaysResult.TotalCount,
            EntriesLast30Days = last30DaysResult.TotalCount
        };

        // Calculate most active users
        var userActivity = periodResult.Items
            .GroupBy(a => a.ActorId)
            .Select(g => new UserActivityDto
            {
                UserId = g.Key.Value,
                ActionCount = g.Count(),
                LastActionUtc = g.Max(a => a.TimestampUtc)
            })
            .OrderByDescending(u => u.ActionCount)
            .Take(10)
            .ToList();

        summary.MostActiveUsers = userActivity;

        // Calculate action breakdown
        var totalActions = periodResult.Items.Count;
        var actionBreakdown = periodResult.Items
            .GroupBy(a => a.Action)
            .Select(g => new ActionBreakdownDto
            {
                Action = g.Key.ToString(),
                Count = g.Count(),
                Percentage = totalActions > 0 ? (decimal)g.Count() / totalActions * 100 : 0
            })
            .OrderByDescending(a => a.Count)
            .ToList();

        summary.ActionBreakdown = actionBreakdown;

        // Calculate entity type breakdown
        var entityTypeBreakdown = periodResult.Items
            .GroupBy(a => a.EntityType)
            .Select(g => new EntityTypeBreakdownDto
            {
                EntityType = g.Key,
                Count = g.Count(),
                Percentage = totalActions > 0 ? (decimal)g.Count() / totalActions * 100 : 0
            })
            .OrderByDescending(e => e.Count)
            .ToList();

        summary.EntityTypeBreakdown = entityTypeBreakdown;

        return summary;
    }
}