using IMS.Api.Application.Common.DTOs;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Service for sending real-time notifications to dashboard clients
/// </summary>
public interface IDashboardNotificationService
{
    /// <summary>
    /// Notify clients about stock level changes for a specific variant and warehouse
    /// </summary>
    /// <param name="variantId">The variant that had a stock level change</param>
    /// <param name="warehouseId">The warehouse where the change occurred</param>
    /// <param name="change">Details of the stock level change</param>
    Task NotifyStockLevelChangeAsync(VariantId variantId, WarehouseId warehouseId, StockLevelChangeDto change);

    /// <summary>
    /// Notify clients about low stock alerts
    /// </summary>
    /// <param name="alert">Low stock alert details</param>
    Task NotifyLowStockAlertAsync(LowStockAlertDto alert);

    /// <summary>
    /// Notify clients about reservation expiry alerts
    /// </summary>
    /// <param name="alert">Reservation expiry alert details</param>
    Task NotifyReservationExpiryAsync(ReservationExpiryAlertDto alert);

    /// <summary>
    /// Notify clients about unusual adjustment patterns
    /// </summary>
    /// <param name="alert">Unusual adjustment alert details</param>
    Task NotifyUnusualAdjustmentAsync(UnusualAdjustmentAlertDto alert);

    /// <summary>
    /// Notify clients about general dashboard metrics updates
    /// </summary>
    /// <param name="metrics">Updated dashboard metrics</param>
    Task NotifyDashboardMetricsUpdateAsync(DashboardMetricsDto metrics);

    /// <summary>
    /// Notify clients about new stock movements
    /// </summary>
    /// <param name="movement">Stock movement details</param>
    Task NotifyStockMovementAsync(StockMovementDto movement);

    /// <summary>
    /// Notify clients about reservation changes
    /// </summary>
    /// <param name="reservation">Reservation details</param>
    Task NotifyReservationChangeAsync(ReservationDto reservation);

    /// <summary>
    /// Send a general alert to all connected clients
    /// </summary>
    /// <param name="alert">General alert details</param>
    Task NotifyGeneralAlertAsync(GeneralAlertDto alert);
}