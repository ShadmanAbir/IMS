using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IMS.Api.Application.Common.Interfaces;

namespace IMS.Api.Infrastructure.Services;

/// <summary>
/// Background service for dashboard cache maintenance
/// Handles cache cleanup, pre-calculation, and refresh operations
/// </summary>
public class DashboardCacheMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DashboardCacheMaintenanceService> _logger;

    // Maintenance intervals
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan PreCalculationInterval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(15);

    public DashboardCacheMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<DashboardCacheMaintenanceService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dashboard cache maintenance service started");

        var lastCleanup = DateTime.MinValue;
        var lastPreCalculation = DateTime.MinValue;
        var lastRefresh = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Cleanup expired cache entries
                if (now - lastCleanup >= CleanupInterval)
                {
                    await PerformCleanupAsync();
                    lastCleanup = now;
                }

                // Pre-calculate common metrics
                if (now - lastPreCalculation >= PreCalculationInterval)
                {
                    await PerformPreCalculationAsync();
                    lastPreCalculation = now;
                }

                // Refresh existing cache entries
                if (now - lastRefresh >= RefreshInterval)
                {
                    await PerformRefreshAsync();
                    lastRefresh = now;
                }

                // Wait before next maintenance cycle
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during cache maintenance");
                
                // Wait longer before retrying after an error
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        _logger.LogInformation("Dashboard cache maintenance service stopped");
    }

    private async Task PerformCleanupAsync()
    {
        try
        {
            _logger.LogDebug("Starting cache cleanup");

            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<IDashboardCacheService>();

            await cacheService.CleanupExpiredCacheAsync();

            _logger.LogDebug("Cache cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform cache cleanup");
        }
    }

    private async Task PerformPreCalculationAsync()
    {
        try
        {
            _logger.LogDebug("Starting cache pre-calculation");

            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<IDashboardCacheService>();

            await cacheService.PreCalculateCommonMetricsAsync();

            _logger.LogDebug("Cache pre-calculation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform cache pre-calculation");
        }
    }

    private async Task PerformRefreshAsync()
    {
        try
        {
            _logger.LogDebug("Starting cache refresh");

            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<IDashboardCacheService>();

            await cacheService.RefreshAllCachedMetricsAsync();

            _logger.LogDebug("Cache refresh completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform cache refresh");
        }
    }
}