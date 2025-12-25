using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Infrastructure.Data;
using System.Text.Json;

namespace IMS.Api.Infrastructure.Services;

/// <summary>
/// Service for managing dashboard metrics caching and materialized views
/// Implements multi-level caching with in-memory and database-backed cache
/// </summary>
public class DashboardCacheService : IDashboardCacheService
{
    private readonly ApplicationDbContext _context;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<DashboardCacheService> _logger;

    // Cache configuration
    private static readonly TimeSpan DefaultCacheExpiry = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MemoryCacheExpiry = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DatabaseCacheExpiry = TimeSpan.FromHours(1);

    public DashboardCacheService(
        ApplicationDbContext context,
        IDashboardRepository dashboardRepository,
        ITenantContext tenantContext,
        IMemoryCache memoryCache,
        ILogger<DashboardCacheService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dashboardRepository = dashboardRepository ?? throw new ArgumentNullException(nameof(dashboardRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DashboardMetricsDto> GetOrCalculateMetricsAsync(
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool includeExpired = false,
        decimal? lowStockThreshold = null,
        string periodType = "hour",
        TimeSpan? cacheExpiry = null)
    {
        if (_tenantContext.CurrentTenantId == null)
            throw new InvalidOperationException("Tenant context is required for caching operations");

        var tenantId = _tenantContext.CurrentTenantId;
        var effectiveFromDate = fromDate ?? DateTime.UtcNow.AddHours(-1);
        var effectiveToDate = toDate ?? DateTime.UtcNow;
        var effectiveCacheExpiry = cacheExpiry ?? DefaultCacheExpiry;

        // Normalize period boundaries based on period type
        var (periodStart, periodEnd) = NormalizePeriod(effectiveFromDate, effectiveToDate, periodType);

        // Generate cache keys
        var warehouseId = warehouseIds?.Count == 1 ? warehouseIds.First() : (Guid?)null;
        var memoryCacheKey = GenerateMemoryCacheKey("metrics", tenantId, warehouseId, periodStart, periodEnd, periodType);
        var databaseCacheKey = DashboardMetricsCache.GetCacheKey(tenantId, warehouseId, periodStart, periodEnd, periodType);

        // Try memory cache first
        if (_memoryCache.TryGetValue(memoryCacheKey, out DashboardMetricsDto? cachedMetrics))
        {
            _logger.LogDebug("Dashboard metrics cache hit (memory): {CacheKey}", memoryCacheKey);
            return cachedMetrics!;
        }

        // Try database cache
        var dbCacheEntry = await _context.DashboardMetricsCache
            .FirstOrDefaultAsync(x => 
                x.TenantId == tenantId &&
                x.WarehouseId == warehouseId &&
                x.PeriodStart == periodStart &&
                x.PeriodEnd == periodEnd &&
                x.PeriodType == periodType &&
                !x.IsExpired);

        if (dbCacheEntry != null)
        {
            _logger.LogDebug("Dashboard metrics cache hit (database): {CacheKey}", databaseCacheKey);
            
            var dbMetrics = MapCacheToDto(dbCacheEntry);
            
            // Store in memory cache for faster subsequent access
            _memoryCache.Set(memoryCacheKey, dbMetrics, MemoryCacheExpiry);
            
            return dbMetrics;
        }

        // Cache miss - calculate metrics
        _logger.LogDebug("Dashboard metrics cache miss, calculating: {CacheKey}", databaseCacheKey);

        var calculatedMetrics = await _dashboardRepository.GetRealTimeMetricsAsync(
            warehouseIds, variantIds, effectiveFromDate, effectiveToDate, includeExpired, lowStockThreshold);

        // Store in database cache
        await StoreDatabaseCacheAsync(tenantId, warehouseId, periodStart, periodEnd, periodType, calculatedMetrics, effectiveCacheExpiry);

        // Store in memory cache
        _memoryCache.Set(memoryCacheKey, calculatedMetrics, MemoryCacheExpiry);

        return calculatedMetrics;
    }

    public async Task<StockMovementRatesDto> GetOrCalculateMovementRatesAsync(
        DateTime fromDate,
        DateTime toDate,
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        List<string>? movementTypes = null,
        string groupBy = "day",
        TimeSpan? cacheExpiry = null)
    {
        if (_tenantContext.CurrentTenantId == null)
            throw new InvalidOperationException("Tenant context is required for caching operations");

        var tenantId = _tenantContext.CurrentTenantId;
        var effectiveCacheExpiry = cacheExpiry ?? DefaultCacheExpiry;

        // Normalize period boundaries
        var (periodStart, periodEnd) = NormalizePeriod(fromDate, toDate, groupBy);

        // For movement rates, we cache by movement type and warehouse
        var warehouseId = warehouseIds?.Count == 1 ? warehouseIds.First() : (Guid?)null;
        var variantId = variantIds?.Count == 1 ? variantIds.First() : (Guid?)null;

        // Generate memory cache key
        var memoryCacheKey = GenerateMemoryCacheKey("movement_rates", tenantId, warehouseId, periodStart, periodEnd, groupBy, 
            string.Join(",", movementTypes ?? new List<string>()), variantId?.ToString());

        // Try memory cache first
        if (_memoryCache.TryGetValue(memoryCacheKey, out StockMovementRatesDto? cachedRates))
        {
            _logger.LogDebug("Movement rates cache hit (memory): {CacheKey}", memoryCacheKey);
            return cachedRates!;
        }

        // For database cache, we need to aggregate multiple cache entries if we have multiple movement types
        var dbCacheEntries = await GetMovementRatesCacheEntriesAsync(tenantId, warehouseId, variantId, 
            movementTypes, periodStart, periodEnd, groupBy);

        if (dbCacheEntries.Any() && AreAllMovementTypesCached(movementTypes, dbCacheEntries))
        {
            _logger.LogDebug("Movement rates cache hit (database): {CacheKey}", memoryCacheKey);
            
            var dbRates = MapMovementRateCacheToDto(dbCacheEntries, periodStart, periodEnd);
            
            // Store in memory cache
            _memoryCache.Set(memoryCacheKey, dbRates, MemoryCacheExpiry);
            
            return dbRates;
        }

        // Cache miss - calculate rates
        _logger.LogDebug("Movement rates cache miss, calculating: {CacheKey}", memoryCacheKey);

        var calculatedRates = await _dashboardRepository.GetStockMovementRatesAsync(
            fromDate, toDate, warehouseIds, variantIds, movementTypes, groupBy);

        // Store in database cache (one entry per movement type)
        await StoreMovementRatesCacheAsync(tenantId, warehouseId, variantId, calculatedRates, 
            periodStart, periodEnd, groupBy, effectiveCacheExpiry);

        // Store in memory cache
        _memoryCache.Set(memoryCacheKey, calculatedRates, MemoryCacheExpiry);

        return calculatedRates;
    }

    public async Task InvalidateCacheAsync(
        List<Guid>? warehouseIds = null,
        List<Guid>? variantIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        if (_tenantContext.CurrentTenantId == null)
            return;

        var tenantId = _tenantContext.CurrentTenantId;

        _logger.LogInformation("Invalidating dashboard cache for tenant {TenantId}", tenantId.Value);

        // Mark database cache entries as stale
        var query = _context.DashboardMetricsCache.Where(x => x.TenantId == tenantId);

        if (warehouseIds?.Any() == true)
        {
            query = query.Where(x => x.WarehouseId == null || warehouseIds.Contains(x.WarehouseId.Value));
        }

        if (fromDate.HasValue && toDate.HasValue)
        {
            query = query.Where(x => x.PeriodStart >= fromDate.Value && x.PeriodEnd <= toDate.Value);
        }

        await query.ForEachAsync(entry => entry.MarkAsStale());

        // Mark movement rates cache as stale
        var movementQuery = _context.StockMovementRatesCache.Where(x => x.TenantId == tenantId);

        if (warehouseIds?.Any() == true)
        {
            movementQuery = movementQuery.Where(x => x.WarehouseId == null || warehouseIds.Contains(x.WarehouseId.Value));
        }

        if (variantIds?.Any() == true)
        {
            movementQuery = movementQuery.Where(x => x.VariantId == null || variantIds.Contains(x.VariantId.Value));
        }

        if (fromDate.HasValue && toDate.HasValue)
        {
            movementQuery = movementQuery.Where(x => x.PeriodStart >= fromDate.Value && x.PeriodEnd <= toDate.Value);
        }

        await movementQuery.ForEachAsync(entry => entry.MarkAsStale());

        await _context.SaveChangesAsync();

        // Clear memory cache (simple approach - clear all for this tenant)
        // In a production system, you might want more granular memory cache invalidation
        _memoryCache.Remove($"tenant_{tenantId.Value}");
    }

    public async Task RefreshAllCachedMetricsAsync()
    {
        if (_tenantContext.CurrentTenantId == null)
            return;

        var tenantId = _tenantContext.CurrentTenantId;

        _logger.LogInformation("Refreshing all cached metrics for tenant {TenantId}", tenantId.Value);

        // Get all non-expired cache entries
        var cacheEntries = await _context.DashboardMetricsCache
            .Where(x => x.TenantId == tenantId && !x.IsExpired)
            .ToListAsync();

        foreach (var entry in cacheEntries)
        {
            try
            {
                var warehouseIds = entry.WarehouseId.HasValue ? new List<Guid> { entry.WarehouseId.Value } : null;
                
                var refreshedMetrics = await _dashboardRepository.GetRealTimeMetricsAsync(
                    warehouseIds, null, entry.PeriodStart, entry.PeriodEnd);

                entry.UpdateMetrics(
                    refreshedMetrics.TotalStockValue,
                    refreshedMetrics.TotalAvailableStock,
                    refreshedMetrics.TotalReservedStock,
                    refreshedMetrics.LowStockVariantCount,
                    refreshedMetrics.OutOfStockVariantCount,
                    refreshedMetrics.ExpiredVariantCount,
                    refreshedMetrics.ExpiringVariantCount,
                    refreshedMetrics.WarehouseBreakdown.Sum(w => w.VariantCount),
                    refreshedMetrics.MovementRates.TotalMovementVolume,
                    refreshedMetrics.MovementRates.TotalMovementCount,
                    refreshedMetrics.MovementRates.AverageMovementSize,
                    DateTime.UtcNow.Add(DatabaseCacheExpiry));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh cache entry {CacheEntryId}", entry.Id);
                entry.MarkAsStale();
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task CleanupExpiredCacheAsync()
    {
        _logger.LogInformation("Cleaning up expired cache entries");

        var expiredMetrics = await _context.DashboardMetricsCache
            .Where(x => x.IsExpired)
            .ToListAsync();

        var expiredRates = await _context.StockMovementRatesCache
            .Where(x => x.IsExpired)
            .ToListAsync();

        _context.DashboardMetricsCache.RemoveRange(expiredMetrics);
        _context.StockMovementRatesCache.RemoveRange(expiredRates);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {MetricsCount} expired metrics entries and {RatesCount} expired rates entries", 
            expiredMetrics.Count, expiredRates.Count);
    }

    public async Task PreCalculateCommonMetricsAsync()
    {
        if (_tenantContext.CurrentTenantId == null)
            return;

        _logger.LogInformation("Pre-calculating common dashboard metrics");

        var now = DateTime.UtcNow;
        var commonPeriods = new[]
        {
            ("hour", now.AddHours(-1), now),
            ("day", now.AddDays(-1), now),
            ("week", now.AddDays(-7), now),
            ("month", now.AddDays(-30), now)
        };

        // Get all warehouses for this tenant
        var warehouses = await _context.Set<Warehouse>()
            .Where(w => w.TenantId == _tenantContext.CurrentTenantId)
            .Select(w => w.Id)
            .ToListAsync();

        foreach (var (periodType, fromDate, toDate) in commonPeriods)
        {
            // Calculate for all warehouses combined
            await GetOrCalculateMetricsAsync(null, null, fromDate, toDate, false, null, periodType);

            // Calculate for each warehouse individually
            foreach (var warehouseId in warehouses)
            {
                await GetOrCalculateMetricsAsync(new List<Guid> { warehouseId }, null, fromDate, toDate, false, null, periodType);
            }
        }
    }

    public async Task<CacheStatisticsDto> GetCacheStatisticsAsync()
    {
        if (_tenantContext.CurrentTenantId == null)
            return new CacheStatisticsDto();

        var tenantId = _tenantContext.CurrentTenantId;

        var metricsStats = await _context.DashboardMetricsCache
            .Where(x => x.TenantId == tenantId)
            .GroupBy(x => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Expired = g.Count(x => x.IsExpired),
                Stale = g.Count(x => x.IsStale)
            })
            .FirstOrDefaultAsync();

        var ratesStats = await _context.StockMovementRatesCache
            .Where(x => x.TenantId == tenantId)
            .CountAsync();

        var entriesByPeriodType = await _context.DashboardMetricsCache
            .Where(x => x.TenantId == tenantId && !x.IsExpired)
            .GroupBy(x => x.PeriodType)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        return new CacheStatisticsDto
        {
            TotalCacheEntries = (metricsStats?.Total ?? 0) + ratesStats,
            ExpiredEntries = metricsStats?.Expired ?? 0,
            StaleEntries = metricsStats?.Stale ?? 0,
            CacheHitRatio = 0, // This would need to be tracked separately
            LastCleanupUtc = DateTime.UtcNow, // This would need to be tracked
            TotalCacheSize = 0, // This would need to be calculated
            EntriesByPeriodType = entriesByPeriodType
        };
    }

    #region Private Helper Methods

    private static (DateTime periodStart, DateTime periodEnd) NormalizePeriod(DateTime fromDate, DateTime toDate, string periodType)
    {
        return periodType.ToLower() switch
        {
            "hour" => (new DateTime(fromDate.Year, fromDate.Month, fromDate.Day, fromDate.Hour, 0, 0, DateTimeKind.Utc),
                      new DateTime(toDate.Year, toDate.Month, toDate.Day, toDate.Hour, 59, 59, DateTimeKind.Utc)),
            "day" => (fromDate.Date, toDate.Date.AddDays(1).AddTicks(-1)),
            "week" => (fromDate.Date.AddDays(-(int)fromDate.DayOfWeek), 
                      toDate.Date.AddDays(6 - (int)toDate.DayOfWeek).AddDays(1).AddTicks(-1)),
            "month" => (new DateTime(fromDate.Year, fromDate.Month, 1),
                       new DateTime(toDate.Year, toDate.Month, DateTime.DaysInMonth(toDate.Year, toDate.Month)).AddDays(1).AddTicks(-1)),
            _ => (fromDate, toDate)
        };
    }

    private static string GenerateMemoryCacheKey(string prefix, TenantId tenantId, params object?[] keyParts)
    {
        var parts = new List<string> { prefix, tenantId.Value.ToString() };
        parts.AddRange(keyParts.Where(p => p != null).Select(p => p!.ToString()!));
        return string.Join("_", parts);
    }

    private async Task StoreDatabaseCacheAsync(TenantId tenantId, Guid? warehouseId, DateTime periodStart, DateTime periodEnd, 
        string periodType, DashboardMetricsDto metrics, TimeSpan cacheExpiry)
    {
        var expiresAt = DateTime.UtcNow.Add(cacheExpiry);
        var totalVariantCount = metrics.WarehouseBreakdown.Sum(w => w.VariantCount);

        var cacheEntry = new DashboardMetricsCache(
            tenantId, warehouseId, periodStart, periodEnd, periodType,
            metrics.TotalStockValue, metrics.TotalAvailableStock, metrics.TotalReservedStock,
            metrics.LowStockVariantCount, metrics.OutOfStockVariantCount, 
            metrics.ExpiredVariantCount, metrics.ExpiringVariantCount, totalVariantCount,
            metrics.MovementRates.TotalMovementVolume, metrics.MovementRates.TotalMovementCount,
            metrics.MovementRates.AverageMovementSize, expiresAt);

        _context.DashboardMetricsCache.Add(cacheEntry);
        await _context.SaveChangesAsync();
    }

    private async Task StoreMovementRatesCacheAsync(TenantId tenantId, Guid? warehouseId, Guid? variantId, 
        StockMovementRatesDto rates, DateTime periodStart, DateTime periodEnd, string periodType, TimeSpan cacheExpiry)
    {
        var expiresAt = DateTime.UtcNow.Add(cacheExpiry);

        foreach (var movementRate in rates.MovementTypeRates)
        {
            var cacheEntry = new StockMovementRatesCache(
                tenantId, warehouseId, variantId, movementRate.MovementType,
                periodStart, periodEnd, periodType,
                movementRate.TotalQuantity, movementRate.MovementCount,
                movementRate.AverageQuantity, movementRate.PercentageOfTotal, expiresAt);

            _context.StockMovementRatesCache.Add(cacheEntry);
        }

        await _context.SaveChangesAsync();
    }

    private async Task<List<StockMovementRatesCache>> GetMovementRatesCacheEntriesAsync(
        TenantId tenantId, Guid? warehouseId, Guid? variantId, List<string>? movementTypes,
        DateTime periodStart, DateTime periodEnd, string periodType)
    {
        var query = _context.StockMovementRatesCache
            .Where(x => x.TenantId == tenantId &&
                       x.WarehouseId == warehouseId &&
                       x.VariantId == variantId &&
                       x.PeriodStart == periodStart &&
                       x.PeriodEnd == periodEnd &&
                       x.PeriodType == periodType &&
                       !x.IsExpired);

        if (movementTypes?.Any() == true)
        {
            query = query.Where(x => movementTypes.Contains(x.MovementType));
        }

        return await query.ToListAsync();
    }

    private static bool AreAllMovementTypesCached(List<string>? requestedTypes, List<StockMovementRatesCache> cachedEntries)
    {
        if (requestedTypes == null || !requestedTypes.Any())
            return cachedEntries.Any();

        return requestedTypes.All(type => cachedEntries.Any(entry => entry.MovementType == type));
    }

    private static DashboardMetricsDto MapCacheToDto(DashboardMetricsCache cache)
    {
        return new DashboardMetricsDto
        {
            TotalStockValue = cache.TotalStockValue,
            TotalAvailableStock = cache.TotalAvailableStock,
            TotalReservedStock = cache.TotalReservedStock,
            LowStockVariantCount = cache.LowStockVariantCount,
            OutOfStockVariantCount = cache.OutOfStockVariantCount,
            ExpiredVariantCount = cache.ExpiredVariantCount,
            ExpiringVariantCount = cache.ExpiringVariantCount,
            WarehouseBreakdown = new List<WarehouseStockDto>(), // Would need separate cache for this
            MovementRates = new StockMovementRatesDto
            {
                PeriodStart = cache.PeriodStart,
                PeriodEnd = cache.PeriodEnd,
                TotalMovementVolume = cache.TotalMovementVolume,
                TotalMovementCount = cache.TotalMovementCount,
                AverageMovementSize = cache.AverageMovementSize,
                MovementTypeRates = new List<MovementTypeRateDto>(),
                WarehouseRates = new List<WarehouseMovementRateDto>()
            },
            GeneratedAtUtc = cache.CalculatedAtUtc
        };
    }

    private static StockMovementRatesDto MapMovementRateCacheToDto(List<StockMovementRatesCache> cacheEntries, DateTime periodStart, DateTime periodEnd)
    {
        var movementTypeRates = cacheEntries
            .GroupBy(x => x.MovementType)
            .Select(g => new MovementTypeRateDto
            {
                MovementType = g.Key,
                TotalQuantity = g.Sum(x => x.TotalQuantity),
                MovementCount = g.Sum(x => x.MovementCount),
                AverageQuantity = g.Average(x => x.AverageQuantity),
                PercentageOfTotal = g.Average(x => x.PercentageOfTotal)
            })
            .ToList();

        return new StockMovementRatesDto
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            MovementTypeRates = movementTypeRates,
            WarehouseRates = new List<WarehouseMovementRateDto>(),
            TotalMovementVolume = movementTypeRates.Sum(x => x.TotalQuantity),
            TotalMovementCount = movementTypeRates.Sum(x => x.MovementCount),
            AverageMovementSize = movementTypeRates.Any() ? movementTypeRates.Average(x => x.AverageQuantity) : 0
        };
    }

    #endregion
}