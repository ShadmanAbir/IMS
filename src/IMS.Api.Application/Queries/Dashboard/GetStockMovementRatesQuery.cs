using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Application.Queries.Dashboard;

/// <summary>
/// Query to get stock movement rates for trend analysis
/// </summary>
public class GetStockMovementRatesQuery : IQuery<Result<StockMovementRatesDto>>
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<Guid>? WarehouseIds { get; set; }
    public List<Guid>? VariantIds { get; set; }
    public List<string>? MovementTypes { get; set; }
    public string GroupBy { get; set; } = "day"; // day, hour, week, month
}

/// <summary>
/// Validator for GetStockMovementRatesQuery
/// </summary>
public class GetStockMovementRatesQueryValidator : AbstractValidator<GetStockMovementRatesQuery>
{
    public GetStockMovementRatesQueryValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty()
            .WithMessage("FromDate is required");

        RuleFor(x => x.ToDate)
            .NotEmpty()
            .WithMessage("ToDate is required");

        RuleFor(x => x.FromDate)
            .LessThan(x => x.ToDate)
            .WithMessage("FromDate must be less than ToDate");

        RuleFor(x => x.ToDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("ToDate cannot be in the future");

        RuleFor(x => x.GroupBy)
            .Must(x => new[] { "hour", "day", "week", "month" }.Contains(x.ToLower()))
            .WithMessage("GroupBy must be one of: hour, day, week, month");

        RuleFor(x => x.MovementTypes)
            .Must(types => types == null || types.All(t => new[] { "Opening", "Purchase", "Sale", "Refund", "Adjustment", "WriteOff", "Transfer" }.Contains(t)))
            .When(x => x.MovementTypes != null)
            .WithMessage("Invalid movement type specified");
    }
}

/// <summary>
/// Handler for GetStockMovementRatesQuery
/// </summary>
public class GetStockMovementRatesQueryHandler : IQueryHandler<GetStockMovementRatesQuery, Result<StockMovementRatesDto>>
{
    private readonly IDashboardCacheService _cacheService;

    public GetStockMovementRatesQueryHandler(IDashboardCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<Result<StockMovementRatesDto>> Handle(GetStockMovementRatesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Determine cache expiry based on time range
            var timeSpan = request.ToDate - request.FromDate;
            var cacheExpiry = timeSpan.TotalDays > 7 ? TimeSpan.FromHours(1) : TimeSpan.FromMinutes(15);

            var movementRates = await _cacheService.GetOrCalculateMovementRatesAsync(
                request.FromDate,
                request.ToDate,
                request.WarehouseIds,
                request.VariantIds,
                request.MovementTypes,
                request.GroupBy,
                cacheExpiry);

            return Result<StockMovementRatesDto>.Success(movementRates);
        }
        catch (Exception ex)
        {
            return Result<StockMovementRatesDto>.Failure("STOCK_MOVEMENT_RATES_ERROR", $"Failed to retrieve stock movement rates: {ex.Message}");
        }
    }
}