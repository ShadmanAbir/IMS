using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Application.Queries.Dashboard;

/// <summary>
/// Query to get real-time dashboard metrics overview
/// </summary>
public class GetRealTimeMetricsQuery : IQuery<Result<DashboardMetricsDto>>
{
    public List<Guid>? WarehouseIds { get; set; }
    public List<Guid>? VariantIds { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IncludeExpired { get; set; } = false;
    public decimal? LowStockThreshold { get; set; }
}

/// <summary>
/// Validator for GetRealTimeMetricsQuery
/// </summary>
public class GetRealTimeMetricsQueryValidator : AbstractValidator<GetRealTimeMetricsQuery>
{
    public GetRealTimeMetricsQueryValidator()
    {
        RuleFor(x => x.FromDate)
            .LessThan(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("FromDate must be less than ToDate");

        RuleFor(x => x.LowStockThreshold)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LowStockThreshold.HasValue)
            .WithMessage("LowStockThreshold must be non-negative");
    }
}

/// <summary>
/// Handler for GetRealTimeMetricsQuery
/// </summary>
public class GetRealTimeMetricsQueryHandler : IQueryHandler<GetRealTimeMetricsQuery, Result<DashboardMetricsDto>>
{
    private readonly IDashboardCacheService _cacheService;

    public GetRealTimeMetricsQueryHandler(IDashboardCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<Result<DashboardMetricsDto>> Handle(GetRealTimeMetricsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Use caching service for improved performance
            var metrics = await _cacheService.GetOrCalculateMetricsAsync(
                request.WarehouseIds,
                request.VariantIds,
                request.FromDate,
                request.ToDate,
                request.IncludeExpired,
                request.LowStockThreshold,
                "hour", // Default to hourly caching for real-time metrics
                TimeSpan.FromMinutes(5)); // Short cache expiry for real-time data

            return Result<DashboardMetricsDto>.Success(metrics);
        }
        catch (Exception ex)
        {
            return Result<DashboardMetricsDto>.Failure("DASHBOARD_METRICS_ERROR", $"Failed to retrieve dashboard metrics: {ex.Message}");
        }
    }
}