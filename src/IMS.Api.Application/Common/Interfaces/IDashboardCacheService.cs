using IMS.Api.Application.Common.DTOs;

namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Service interface for dashboard metrics caching and materialized view management
/// </summary>
public interface IDashboardCacheService
{
    /// <summary>
    /// Gets cached dashboard metrics or calculates and caches them if not available
    /// </summary>
    Task<DashboardMetricsDto> GetOrCalculateMetricsAsync(
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool includeExpired = false,
        decimal? lowStockThreshold = null,
        string periodType = "hour",
        TimeSpan? cacheExpiry = null);

    /// <summary>
    /// Gets cached stock movement rates or calculates and caches them if not available
    /// </summary>
    Task<StockMovementRatesDto> GetOrCalculateMovementRatesAsync(
        DateTime fromDate,
        DateTime toDate,
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        List<string>? movementTypes = null,
        string groupBy = "day",
        TimeSpan? cacheExpiry = null);

    /// <summary>
    /// Invalidates cached metrics for specific warehouses or variants
    /// </summary>
    Task InvalidateCacheAsync(
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Refreshes all cached metrics in the background
    /// </summary>
    Task RefreshAllCachedMetricsAsync();

    /// <summary>
    /// Cleans up expired cache entries
    /// </summary>
    Task CleanupExpiredCacheAsync();

    /// <summary>
    /// Pre-calculates and caches metrics for common time periods
    /// </summary>
    Task PreCalculateCommonMetricsAsync();

    /// <summary>
    /// Gets cache statistics for monitoring
    /// </summary>
    Task<CacheStatisticsDto> GetCacheStatisticsAsync();
}

/// <summary>
/// DTO for cache statistics
/// </summary>
public class CacheStatisticsDto
{
    public int TotalCacheEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public int StaleEntries { get; set; }
    public decimal CacheHitRatio { get; set; }
    public DateTime LastCleanupUtc { get; set; }
    public long TotalCacheSize { get; set; }
    public Dictionary<string, int> EntriesByPeriodType { get; set; } = new();
    public Dictionary<string, decimal> HitRatioByPeriodType { get; set; } = new();
}