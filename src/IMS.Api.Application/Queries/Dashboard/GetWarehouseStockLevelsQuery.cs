using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Application.Queries.Dashboard;

/// <summary>
/// Query to get warehouse-specific stock levels for dashboard
/// </summary>
public class GetWarehouseStockLevelsQuery : IQuery<Result<List<WarehouseStockDto>>>
{
    public List<Guid>? WarehouseIds { get; set; }
    public List<Guid>? VariantIds { get; set; }
    public bool IncludeExpired { get; set; } = false;
    public decimal? LowStockThreshold { get; set; }
    public bool IncludeEmptyWarehouses { get; set; } = true;
}

/// <summary>
/// Validator for GetWarehouseStockLevelsQuery
/// </summary>
public class GetWarehouseStockLevelsQueryValidator : AbstractValidator<GetWarehouseStockLevelsQuery>
{
    public GetWarehouseStockLevelsQueryValidator()
    {
        RuleFor(x => x.LowStockThreshold)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LowStockThreshold.HasValue)
            .WithMessage("LowStockThreshold must be non-negative");
    }
}

/// <summary>
/// Handler for GetWarehouseStockLevelsQuery
/// </summary>
public class GetWarehouseStockLevelsQueryHandler : IQueryHandler<GetWarehouseStockLevelsQuery, Result<List<WarehouseStockDto>>>
{
    private readonly IDashboardRepository _dashboardRepository;

    public GetWarehouseStockLevelsQueryHandler(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<Result<List<WarehouseStockDto>>> Handle(GetWarehouseStockLevelsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var warehouseStockLevels = await _dashboardRepository.GetWarehouseStockLevelsAsync(
                request.WarehouseIds,
                request.VariantIds,
                request.IncludeExpired,
                request.LowStockThreshold,
                request.IncludeEmptyWarehouses);

            return Result<List<WarehouseStockDto>>.Success(warehouseStockLevels);
        }
        catch (Exception ex)
        {
            return Result<List<WarehouseStockDto>>.Failure("WAREHOUSE_STOCK_LEVELS_ERROR", $"Failed to retrieve warehouse stock levels: {ex.Message}");
        }
    }
}