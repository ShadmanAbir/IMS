using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Queries.Dashboard;
using IMS.Api.Application.Queries.Inventory;
using MediatR;

namespace IMS.Api.Infrastructure.Services;

/// <summary>
/// Background service for detecting and sending operational alerts
/// </summary>
public class AlertDetectionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlertDetectionBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2); // Check every 2 minutes

    public AlertDetectionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AlertDetectionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Alert Detection Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DetectAndSendAlertsAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while detecting alerts");
                
                // Wait a bit longer before retrying on error
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Alert Detection Background Service stopped");
    }

    private async Task DetectAndSendAlertsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IDashboardNotificationService>();

        try
        {
            // Detect low stock alerts
            await DetectLowStockAlertsAsync(mediator, notificationService, cancellationToken);

            // Detect unusual adjustment patterns
            await DetectUnusualAdjustmentPatternsAsync(mediator, notificationService, cancellationToken);

            _logger.LogDebug("Alert detection cycle completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect and send alerts");
            throw;
        }
    }

    private async Task DetectLowStockAlertsAsync(IMediator mediator, IDashboardNotificationService notificationService, CancellationToken cancellationToken)
    {
        try
        {
            // Get low stock variants
            var lowStockQuery = new GetLowStockVariantsQuery
            {
                PageNumber = 1,
                PageSize = 100 // Process in batches
            };

            var lowStockVariants = await mediator.Send(lowStockQuery, cancellationToken);

            if (lowStockVariants?.Items?.Any() == true)
            {
                foreach (var variant in lowStockVariants.Items)
                {
                    var severity = DetermineLowStockSeverity(variant.AvailableStock, variant.LowStockThreshold);
                    
                    var alert = new LowStockAlertDto
                    {
                        Id = Guid.NewGuid(),
                        VariantId = variant.VariantId,
                        SKU = variant.SKU,
                        VariantName = variant.VariantName,
                        WarehouseId = variant.WarehouseId,
                        WarehouseName = variant.WarehouseName,
                        CurrentStock = variant.AvailableStock,
                        LowStockThreshold = variant.LowStockThreshold,
                        StockDeficit = variant.LowStockThreshold - variant.AvailableStock,
                        Severity = severity,
                        DetectedAtUtc = DateTime.UtcNow
                    };

                    await notificationService.NotifyLowStockAlertAsync(alert);
                }

                _logger.LogInformation("Sent {Count} low stock alerts", lowStockVariants.Items.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect low stock alerts");
        }
    }

    private async Task DetectUnusualAdjustmentPatternsAsync(IMediator mediator, IDashboardNotificationService notificationService, CancellationToken cancellationToken)
    {
        try
        {
            // Get stock movement rates for the last hour to detect unusual patterns
            var movementRatesQuery = new GetStockMovementRatesQuery
            {
                PeriodStart = DateTime.UtcNow.AddHours(-1),
                PeriodEnd = DateTime.UtcNow
            };

            var movementRates = await mediator.Send(movementRatesQuery, cancellationToken);

            if (movementRates?.MovementTypeRates?.Any() == true)
            {
                // Check for unusual adjustment patterns
                var adjustmentRate = movementRates.MovementTypeRates
                    .FirstOrDefault(r => r.MovementType.Equals("Adjustment", StringComparison.OrdinalIgnoreCase));

                if (adjustmentRate != null && IsUnusualAdjustmentPattern(adjustmentRate))
                {
                    var alert = new UnusualAdjustmentAlertDto
                    {
                        Id = Guid.NewGuid(),
                        PatternType = "FrequencySpike",
                        AdjustmentAmount = adjustmentRate.TotalQuantity,
                        AdjustmentCount = adjustmentRate.MovementCount,
                        PeriodStart = movementRatesQuery.PeriodStart,
                        PeriodEnd = movementRatesQuery.PeriodEnd,
                        Description = $"Detected {adjustmentRate.MovementCount} adjustments totaling {adjustmentRate.TotalQuantity:N2} units in the last hour, which is above normal patterns.",
                        DetectedAtUtc = DateTime.UtcNow
                    };

                    await notificationService.NotifyUnusualAdjustmentAsync(alert);
                    
                    _logger.LogInformation("Sent unusual adjustment pattern alert. Count: {Count}, Total: {Total}",
                        adjustmentRate.MovementCount, adjustmentRate.TotalQuantity);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect unusual adjustment patterns");
        }
    }

    private static string DetermineLowStockSeverity(decimal currentStock, decimal threshold)
    {
        if (currentStock <= 0)
            return "OutOfStock";
        
        if (currentStock <= threshold * 0.25m)
            return "Critical";
        
        if (currentStock <= threshold * 0.5m)
            return "Low";
        
        return "Warning";
    }

    private static bool IsUnusualAdjustmentPattern(MovementTypeRateDto adjustmentRate)
    {
        // Consider it unusual if there are more than 10 adjustments in an hour
        // or if the total adjustment amount is greater than 1000 units
        return adjustmentRate.MovementCount > 10 || Math.Abs(adjustmentRate.TotalQuantity) > 1000;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Alert Detection Background Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}