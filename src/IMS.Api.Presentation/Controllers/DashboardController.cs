using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using IMS.Api.Application.Queries.Dashboard;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Dashboard controller for real-time metrics and operational alerts
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DashboardController : BaseController
{
    public DashboardController(IMediator mediator) : base(mediator)
    {
    }

    /// <summary>
    /// Gets real-time dashboard metrics overview
    /// </summary>
    /// <param name="warehouseIds">Optional warehouse filter</param>
    /// <param name="variantIds">Optional variant filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="includeExpired">Include expired items in metrics</param>
    /// <param name="lowStockThreshold">Custom low stock threshold</param>
    /// <returns>Dashboard metrics</returns>
    [HttpGet("metrics")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<ApiResponse<DashboardMetricsDto>>> GetRealTimeMetrics(
        [FromQuery] List<Guid>? warehouseIds = null,
        [FromQuery] List<Guid>? variantIds = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] bool includeExpired = false,
        [FromQuery] decimal? lowStockThreshold = null)
    {
        var query = new GetRealTimeMetricsQuery
        {
            WarehouseIds = warehouseIds,
            VariantIds = variantIds,
            FromDate = fromDate,
            ToDate = toDate,
            IncludeExpired = includeExpired,
            LowStockThreshold = lowStockThreshold
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets warehouse-specific stock levels for dashboard
    /// </summary>
    /// <param name="warehouseIds">Optional warehouse filter</param>
    /// <param name="variantIds">Optional variant filter</param>
    /// <param name="includeExpired">Include expired items</param>
    /// <param name="lowStockThreshold">Custom low stock threshold</param>
    /// <param name="includeEmptyWarehouses">Include warehouses with no stock</param>
    /// <returns>Warehouse stock levels</returns>
    [HttpGet("warehouse-stock")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<ApiResponse<List<WarehouseStockDto>>>> GetWarehouseStockLevels(
        [FromQuery] List<Guid>? warehouseIds = null,
        [FromQuery] List<Guid>? variantIds = null,
        [FromQuery] bool includeExpired = false,
        [FromQuery] decimal? lowStockThreshold = null,
        [FromQuery] bool includeEmptyWarehouses = true)
    {
        var query = new GetWarehouseStockLevelsQuery
        {
            WarehouseIds = warehouseIds,
            VariantIds = variantIds,
            IncludeExpired = includeExpired,
            LowStockThreshold = lowStockThreshold,
            IncludeEmptyWarehouses = includeEmptyWarehouses
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets stock movement rates for trend analysis
    /// </summary>
    /// <param name="fromDate">Start date for analysis period</param>
    /// <param name="toDate">End date for analysis period</param>
    /// <param name="warehouseIds">Optional warehouse filter</param>
    /// <param name="variantIds">Optional variant filter</param>
    /// <param name="movementTypes">Optional movement type filter</param>
    /// <param name="groupBy">Grouping period (hour, day, week, month)</param>
    /// <returns>Stock movement rates and trends</returns>
    [HttpGet("movement-rates")]
    [Authorize(Policy = "RequireInventoryAnalyst")]
    public async Task<ActionResult<ApiResponse<StockMovementRatesDto>>> GetStockMovementRates(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] List<Guid>? warehouseIds = null,
        [FromQuery] List<Guid>? variantIds = null,
        [FromQuery] List<string>? movementTypes = null,
        [FromQuery] string groupBy = "day")
    {
        var query = new GetStockMovementRatesQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            WarehouseIds = warehouseIds,
            VariantIds = variantIds,
            MovementTypes = movementTypes,
            GroupBy = groupBy
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets active operational alerts
    /// </summary>
    /// <param name="alertTypes">Optional alert type filter</param>
    /// <param name="severities">Optional severity filter</param>
    /// <param name="warehouseIds">Optional warehouse filter</param>
    /// <param name="variantIds">Optional variant filter</param>
    /// <param name="includeAcknowledged">Include acknowledged alerts</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDirection">Sort direction (asc/desc)</param>
    /// <returns>Paginated list of active alerts</returns>
    [HttpGet("alerts")]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult<ApiResponse<PagedResult<AlertDto>>>> GetActiveAlerts(
        [FromQuery] List<string>? alertTypes = null,
        [FromQuery] List<string>? severities = null,
        [FromQuery] List<Guid>? warehouseIds = null,
        [FromQuery] List<Guid>? variantIds = null,
        [FromQuery] bool includeAcknowledged = false,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string sortBy = "CreatedAtUtc",
        [FromQuery] string sortDirection = "desc")
    {
        var query = new GetActiveAlertsQuery
        {
            AlertTypes = alertTypes,
            Severities = severities,
            WarehouseIds = warehouseIds,
            VariantIds = variantIds,
            IncludeAcknowledged = includeAcknowledged,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Acknowledges an alert
    /// </summary>
    /// <param name="alertId">Alert ID to acknowledge</param>
    /// <returns>Success result</returns>
    [HttpPatch("alerts/{alertId:guid}/acknowledge")]
    [Authorize(Policy = "RequireWarehouseOperator")]
    public async Task<ActionResult<ApiResponse<bool>>> AcknowledgeAlert(Guid alertId)
    {
        // This would typically be implemented as a command handler
        // For now, we'll return a placeholder response
        var result = Result<bool>.Success(true);
        return HandleResult(result);
    }

    /// <summary>
    /// Deactivates an alert
    /// </summary>
    /// <param name="alertId">Alert ID to deactivate</param>
    /// <returns>Success result</returns>
    [HttpPatch("alerts/{alertId:guid}/deactivate")]
    [Authorize(Policy = "RequireWarehouseManager")]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateAlert(Guid alertId)
    {
        // This would typically be implemented as a command handler
        // For now, we'll return a placeholder response
        var result = Result<bool>.Success(true);
        return HandleResult(result);
    }
}