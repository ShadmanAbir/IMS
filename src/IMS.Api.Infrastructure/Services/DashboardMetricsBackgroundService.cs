using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Queries.Dashboard;
using MediatR;

namespace IMS.Api.Infrastructure.Services;

/// <summary>
/// Background service for calculating and broadcasting dashboard metrics
/// </summary>
public class DashboardMetricsBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DashboardMetricsBackgroundService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(30); // Update every 30 seconds

    public DashboardMetricsBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DashboardMetricsBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dashboard Metrics Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateDashboardMetricsAsync(stoppingToken);
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating dashboard metrics");
                
                // Wait a bit longer before retrying on error
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Dashboard Metrics Background Service stopped");
    }

    private async Task UpdateDashboardMetricsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IDashboardNotificationService>();

        try
        {
            // Get real-time metrics
            var metricsQuery = new GetRealTimeMetricsQuery();
            var metrics = await mediator.Send(metricsQuery, cancellationToken);

            if (metrics != null)
            {
                // Broadcast updated metrics to all dashboard subscribers
                await notificationService.NotifyDashboardMetricsUpdateAsync(metrics);

                _logger.LogDebug("Dashboard metrics updated and broadcasted. Total stock value: {TotalStockValue}, Low stock variants: {LowStockCount}",
                    metrics.TotalStockValue, metrics.LowStockVariantCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update dashboard metrics");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Dashboard Metrics Background Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}