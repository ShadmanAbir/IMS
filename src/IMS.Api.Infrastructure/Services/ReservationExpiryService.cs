using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.Enums;
using IMS.Api.Infrastructure.Data;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IMS.Api.Infrastructure.Services;

/// <summary>
/// Background service that automatically expires reservations that have passed their expiry date
/// </summary>
public class ReservationExpiryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationExpiryService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

    public ReservationExpiryService(
        IServiceProvider serviceProvider,
        ILogger<ReservationExpiryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reservation Expiry Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);
                await CheckExpiringReservationsAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing expired reservations");
                // Continue running even if there's an error
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        _logger.LogInformation("Reservation Expiry Service stopped");
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IDashboardNotificationService>();

        try
        {
            var cutoffTime = DateTime.UtcNow;
            
            // Find active reservations that have expired
            var expiredReservations = await dbContext.Set<Reservation>()
                .Include(r => r.Variant)
                    .ThenInclude(v => v.Product)
                .Include(r => r.Warehouse)
                .Where(r => !r.IsDeleted &&
                           (r.Status == ReservationStatus.Active || r.Status == ReservationStatus.PartiallyFulfilled) &&
                           r.ExpiresAtUtc <= cutoffTime)
                .ToListAsync(cancellationToken);

            if (expiredReservations.Any())
            {
                _logger.LogInformation("Found {Count} expired reservations to process", expiredReservations.Count);

                foreach (var reservation in expiredReservations)
                {
                    try
                    {
                        reservation.Expire();
                        
                        // Send real-time notification about expired reservation
                        var alert = new ReservationExpiryAlertDto
                        {
                            Id = Guid.NewGuid(),
                            ReservationId = reservation.Id.Value,
                            VariantId = reservation.VariantId.Value,
                            SKU = reservation.Variant?.Sku?.Value ?? "Unknown",
                            VariantName = reservation.Variant?.Name ?? "Unknown",
                            WarehouseId = reservation.WarehouseId.Value,
                            WarehouseName = reservation.Warehouse?.Name ?? "Unknown",
                            ReservedQuantity = reservation.Quantity,
                            ExpiresAtUtc = reservation.ExpiresAtUtc,
                            ReferenceNumber = reservation.ReferenceNumber ?? string.Empty,
                            AlertType = "Expired",
                            DetectedAtUtc = DateTime.UtcNow
                        };

                        await notificationService.NotifyReservationExpiryAsync(alert);
                        
                        _logger.LogDebug("Expired reservation {ReservationId} for variant {VariantId} and sent notification", 
                            reservation.Id, reservation.VariantId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to expire reservation {ReservationId}", reservation.Id);
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully expired {Count} reservations", expiredReservations.Count);
            }
            else
            {
                _logger.LogDebug("No expired reservations found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while querying for expired reservations");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reservation Expiry Service is stopping");
        await base.StopAsync(cancellationToken);
    }

    private async Task CheckExpiringReservationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IDashboardNotificationService>();

        try
        {
            var warningTime = DateTime.UtcNow.AddMinutes(30); // Warn 30 minutes before expiry
            var currentTime = DateTime.UtcNow;
            
            // Find active reservations that will expire within the warning period
            var expiringReservations = await dbContext.Set<Reservation>()
                .Include(r => r.Variant)
                    .ThenInclude(v => v.Product)
                .Include(r => r.Warehouse)
                .Where(r => !r.IsDeleted &&
                           (r.Status == ReservationStatus.Active || r.Status == ReservationStatus.PartiallyFulfilled) &&
                           r.ExpiresAtUtc > currentTime &&
                           r.ExpiresAtUtc <= warningTime)
                .ToListAsync(cancellationToken);

            if (expiringReservations.Any())
            {
                _logger.LogInformation("Found {Count} reservations expiring within 30 minutes", expiringReservations.Count);

                foreach (var reservation in expiringReservations)
                {
                    try
                    {
                        // Send real-time notification about expiring reservation
                        var alert = new ReservationExpiryAlertDto
                        {
                            Id = Guid.NewGuid(),
                            ReservationId = reservation.Id.Value,
                            VariantId = reservation.VariantId.Value,
                            SKU = reservation.Variant?.Sku?.Value ?? "Unknown",
                            VariantName = reservation.Variant?.Name ?? "Unknown",
                            WarehouseId = reservation.WarehouseId.Value,
                            WarehouseName = reservation.Warehouse?.Name ?? "Unknown",
                            ReservedQuantity = reservation.Quantity,
                            ExpiresAtUtc = reservation.ExpiresAtUtc,
                            ReferenceNumber = reservation.ReferenceNumber ?? string.Empty,
                            AlertType = "Expiring",
                            DetectedAtUtc = DateTime.UtcNow
                        };

                        await notificationService.NotifyReservationExpiryAsync(alert);
                        
                        _logger.LogDebug("Sent expiring reservation alert for reservation {ReservationId} for variant {VariantId}", 
                            reservation.Id, reservation.VariantId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send expiring reservation alert for reservation {ReservationId}", reservation.Id);
                    }
                }
            }
            else
            {
                _logger.LogDebug("No reservations expiring within 30 minutes found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking for expiring reservations");
        }
    }
}