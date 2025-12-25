using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Domain.Enums;

namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Reservation aggregate
/// </summary>
public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(ReservationId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Reservation>> GetByIdsAsync(List<ReservationId> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Reservation entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Reservation entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Reservation entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets reservations by reference number
    /// </summary>
    /// <param name="referenceNumber">Reference number to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of reservations with the specified reference number</returns>
    Task<List<Reservation>> GetByReferenceNumberAsync(string referenceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active reservations for a variant in a warehouse
    /// </summary>
    /// <param name="variantId">Variant identifier</param>
    /// <param name="warehouseId">Warehouse identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active reservations</returns>
    Task<List<Reservation>> GetActiveReservationsAsync(
        VariantId variantId,
        WarehouseId warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired reservations that need to be processed
    /// </summary>
    /// <param name="asOfDate">Date to check expiry against (default: current UTC time)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expired reservations</returns>
    Task<List<Reservation>> GetExpiredReservationsAsync(
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of reservations with optional filtering
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="variantId">Optional variant filter</param>
    /// <param name="warehouseId">Optional warehouse filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="referenceNumber">Optional reference number filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="includeExpired">Whether to include expired reservations</param>
    /// <param name="includeDeleted">Whether to include soft-deleted reservations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of reservations</returns>
    Task<PagedResult<Reservation>> GetPagedAsync(
        int page,
        int pageSize,
        VariantId? variantId = null,
        WarehouseId? warehouseId = null,
        ReservationStatus? status = null,
        string? referenceNumber = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool includeExpired = true,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets reservation statistics for monitoring
    /// </summary>
    /// <param name="warehouseId">Optional warehouse filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reservation statistics</returns>
    Task<ReservationStatistics> GetStatisticsAsync(
        WarehouseId? warehouseId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics for reservation monitoring
/// </summary>
public class ReservationStatistics
{
    public int TotalReservations { get; set; }
    public int ActiveReservations { get; set; }
    public int ExpiredReservations { get; set; }
    public int UsedReservations { get; set; }
    public int CancelledReservations { get; set; }
    public decimal TotalReservedQuantity { get; set; }
    public decimal AverageReservationDuration { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
}