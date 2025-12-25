using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Application.Queries.Dashboard;

/// <summary>
/// Query to get active operational alerts
/// </summary>
public class GetActiveAlertsQuery : IQuery<Result<PagedResult<AlertDto>>>
{
    public List<string>? AlertTypes { get; set; }
    public List<string>? Severities { get; set; }
    public List<Guid>? WarehouseIds { get; set; }
    public List<Guid>? VariantIds { get; set; }
    public bool IncludeAcknowledged { get; set; } = false;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = "CreatedAtUtc";
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// Validator for GetActiveAlertsQuery
/// </summary>
public class GetActiveAlertsQueryValidator : AbstractValidator<GetActiveAlertsQuery>
{
    public GetActiveAlertsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.FromDate)
            .LessThan(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("FromDate must be less than ToDate");

        RuleFor(x => x.SortBy)
            .Must(x => new[] { "CreatedAtUtc", "Severity", "AlertType", "VariantSKU", "WarehouseName" }.Contains(x))
            .WithMessage("Invalid SortBy field");

        RuleFor(x => x.SortDirection)
            .Must(x => new[] { "asc", "desc" }.Contains(x.ToLower()))
            .WithMessage("SortDirection must be 'asc' or 'desc'");

        RuleFor(x => x.AlertTypes)
            .Must(types => types == null || types.All(t => new[] { "LowStock", "OutOfStock", "Expired", "Expiring", "UnusualAdjustment", "ReservationExpiry", "SystemError" }.Contains(t)))
            .When(x => x.AlertTypes != null)
            .WithMessage("Invalid alert type specified");

        RuleFor(x => x.Severities)
            .Must(severities => severities == null || severities.All(s => new[] { "Low", "Medium", "High", "Critical" }.Contains(s)))
            .When(x => x.Severities != null)
            .WithMessage("Invalid severity level specified");
    }
}

/// <summary>
/// Handler for GetActiveAlertsQuery
/// </summary>
public class GetActiveAlertsQueryHandler : IQueryHandler<GetActiveAlertsQuery, Result<PagedResult<AlertDto>>>
{
    private readonly IDashboardRepository _dashboardRepository;

    public GetActiveAlertsQueryHandler(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<Result<PagedResult<AlertDto>>> Handle(GetActiveAlertsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var alerts = await _dashboardRepository.GetActiveAlertsAsync(
                request.AlertTypes,
                request.Severities,
                request.WarehouseIds,
                request.VariantIds,
                request.IncludeAcknowledged,
                request.FromDate,
                request.ToDate,
                request.Page,
                request.PageSize,
                request.SortBy,
                request.SortDirection);

            return Result<PagedResult<AlertDto>>.Success(alerts);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<AlertDto>>.Failure("ACTIVE_ALERTS_ERROR", $"Failed to retrieve active alerts: {ex.Message}");
        }
    }
}