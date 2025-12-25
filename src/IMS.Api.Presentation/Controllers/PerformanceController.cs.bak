using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Infrastructure.Services;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Controller for performance monitoring and cache management
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireSystemAdmin")]
public class PerformanceController : ControllerBase
{
    private readonly IQueryPerformanceMonitoringService _performanceMonitoring;
    private readonly IDashboardCacheService _cacheService;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(
        IQueryPerformanceMonitoringService performanceMonitoring,
        IDashboardCacheService cacheService,
        ILogger<PerformanceController> logger)
    {
        _performanceMonitoring = performanceMonitoring ?? throw new ArgumentNullException(nameof(performanceMonitoring));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets query performance report
    /// </summary>
    [HttpGet("query-performance")]
    public ActionResult<QueryPerformanceReport> GetQueryPerformanceReport()
    {
        try
        {
            var report = _performanceMonitoring.GetPerformanceReport();
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get query performance report");
            return StatusCode(500, "Failed to retrieve performance report");
        }
    }

    /// <summary>
    /// Resets query performance metrics
    /// </summary>
    [HttpPost("query-performance/reset")]
    public IActionResult ResetQueryPerformanceMetrics()
    {
        try
        {
            _performanceMonitoring.ResetMetrics();
            _logger.LogInformation("Query performance metrics reset by user");
            return Ok(new { message = "Query performance metrics reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset query performance metrics");
            return StatusCode(500, "Failed to reset performance metrics");
        }
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    [HttpGet("cache/statistics")]
    public async Task<ActionResult<CacheStatisticsDto>> GetCacheStatistics()
    {
        try
        {
            var statistics = await _cacheService.GetCacheStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache statistics");
            return StatusCode(500, "Failed to retrieve cache statistics");
        }
    }

    /// <summary>
    /// Invalidates dashboard cache
    /// </summary>
    [HttpPost("cache/invalidate")]
    public async Task<IActionResult> InvalidateCache(
        [FromQuery] List<Guid>? warehouseIds = null,
        [FromQuery] List<Guid>? variantIds = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            await _cacheService.InvalidateCacheAsync(warehouseIds, variantIds, fromDate, toDate);
            _logger.LogInformation("Cache invalidated by user for warehouses: {WarehouseIds}, variants: {VariantIds}", 
                warehouseIds, variantIds);
            return Ok(new { message = "Cache invalidated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache");
            return StatusCode(500, "Failed to invalidate cache");
        }
    }

    /// <summary>
    /// Refreshes all cached metrics
    /// </summary>
    [HttpPost("cache/refresh")]
    public async Task<IActionResult> RefreshCache()
    {
        try
        {
            await _cacheService.RefreshAllCachedMetricsAsync();
            _logger.LogInformation("Cache refresh initiated by user");
            return Ok(new { message = "Cache refresh initiated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh cache");
            return StatusCode(500, "Failed to refresh cache");
        }
    }

    /// <summary>
    /// Cleans up expired cache entries
    /// </summary>
    [HttpPost("cache/cleanup")]
    public async Task<IActionResult> CleanupExpiredCache()
    {
        try
        {
            await _cacheService.CleanupExpiredCacheAsync();
            _logger.LogInformation("Cache cleanup initiated by user");
            return Ok(new { message = "Cache cleanup completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired cache");
            return StatusCode(500, "Failed to cleanup expired cache");
        }
    }

    /// <summary>
    /// Pre-calculates common dashboard metrics
    /// </summary>
    [HttpPost("cache/precalculate")]
    public async Task<IActionResult> PreCalculateMetrics()
    {
        try
        {
            await _cacheService.PreCalculateCommonMetricsAsync();
            _logger.LogInformation("Cache pre-calculation initiated by user");
            return Ok(new { message = "Cache pre-calculation initiated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pre-calculate metrics");
            return StatusCode(500, "Failed to pre-calculate metrics");
        }
    }
}