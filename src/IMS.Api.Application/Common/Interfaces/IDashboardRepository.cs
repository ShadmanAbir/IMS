using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Repository interface for dashboard-related data operations
/// </summary>
public interface IDashboardRepository
{
    /// <summary>
    /// Gets real-time dashboard metrics overview
    /// </summary>
    Task<DashboardMetricsDto> GetRealTimeMetricsAsync(
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool includeExpired = false,
        decimal? lowStockThreshold = null);

    /// <summary>
    /// Gets warehouse-specific stock levels
    /// </summary>
    Task<List<WarehouseStockDto>> GetWarehouseStockLevelsAsync(
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        bool includeExpired = false,
        decimal? lowStockThreshold = null,
        bool includeEmptyWarehouses = true);

    /// <summary>
    /// Gets stock movement rates for trend analysis
    /// </summary>
    Task<StockMovementRatesDto> GetStockMovementRatesAsync(
        DateTime fromDate,
        DateTime toDate,
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        List<string>? movementTypes = null,
        string groupBy = "day");

    /// <summary>
    /// Gets active operational alerts
    /// </summary>
    Task<PagedResult<AlertDto>> GetActiveAlertsAsync(
        List<string>? alertTypes = null,
        List<string>? severities = null,
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        bool includeAcknowledged = false,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50,
        string sortBy = "CreatedAtUtc",
        string sortDirection = "desc");

    /// <summary>
    /// Creates or updates an alert
    /// </summary>
    Task<AlertDto> CreateOrUpdateAlertAsync(
        string alertType,
        string severity,
        string title,
        string message,
        Guid? variantId = null,
        Guid? warehouseId = null,
        Dictionary<string, object>? data = null);

    /// <summary>
    /// Acknowledges an alert
    /// </summary>
    Task<bool> AcknowledgeAlertAsync(Guid alertId, Guid acknowledgedBy);

    /// <summary>
    /// Deactivates an alert
    /// </summary>
    Task<bool> DeactivateAlertAsync(Guid alertId);

    /// <summary>
    /// Gets dashboard metrics for a specific time period (for caching/materialized views)
    /// </summary>
    Task<DashboardMetricsDto> GetCachedMetricsAsync(
        DateTime periodStart,
        DateTime periodEnd,
        List<Guid>? warehouseIds = null);

    /// <summary>
    /// Updates cached dashboard metrics
    /// </summary>
    Task UpdateCachedMetricsAsync(
        DateTime periodStart,
        DateTime periodEnd,
        DashboardMetricsDto metrics);
}