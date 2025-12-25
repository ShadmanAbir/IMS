using IMS.Api.Domain.Entities;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Service interface for audit logging functionality
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs an audit entry for entity changes
    /// </summary>
    /// <param name="action">The action performed</param>
    /// <param name="entityType">The type of entity affected</param>
    /// <param name="entityId">The ID of the entity affected</param>
    /// <param name="description">Description of the action</param>
    /// <param name="oldValues">Previous state of the entity</param>
    /// <param name="newValues">New state of the entity</param>
    /// <param name="warehouseId">Warehouse ID if applicable</param>
    /// <param name="variantId">Variant ID if applicable</param>
    /// <param name="reason">Reason for the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task LogAsync(
        AuditAction action,
        string entityType,
        string? entityId,
        string description,
        object? oldValues = null,
        object? newValues = null,
        WarehouseId? warehouseId = null,
        VariantId? variantId = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an audit entry for stock movements
    /// </summary>
    /// <param name="stockMovement">The stock movement that was recorded</param>
    /// <param name="warehouseId">The warehouse where the movement occurred</param>
    /// <param name="variantId">The variant that was moved</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task LogStockMovementAsync(
        StockMovement stockMovement,
        WarehouseId warehouseId,
        VariantId variantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an audit entry for user actions
    /// </summary>
    /// <param name="action">The action performed</param>
    /// <param name="description">Description of the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task LogUserActionAsync(
        AuditAction action,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an audit entry for reservation actions
    /// </summary>
    /// <param name="action">The reservation action performed</param>
    /// <param name="reservationId">The reservation ID</param>
    /// <param name="variantId">The variant ID</param>
    /// <param name="warehouseId">The warehouse ID</param>
    /// <param name="description">Description of the action</param>
    /// <param name="oldValues">Previous state of the reservation</param>
    /// <param name="newValues">New state of the reservation</param>
    /// <param name="reason">Reason for the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task LogReservationActionAsync(
        AuditAction action,
        ReservationId reservationId,
        VariantId variantId,
        WarehouseId warehouseId,
        string description,
        object? oldValues = null,
        object? newValues = null,
        string? reason = null,
        CancellationToken cancellationToken = default);
}