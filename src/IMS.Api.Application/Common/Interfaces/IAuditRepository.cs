using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Repository interface for audit log operations
/// </summary>
public interface IAuditRepository
{
    /// <summary>
    /// Adds a new audit log entry
    /// </summary>
    /// <param name="auditLog">The audit log entry to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs with filtering and pagination
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of audit logs</returns>
    Task<PagedResult<AuditLog>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific entity
    /// </summary>
    /// <param name="entityType">The entity type</param>
    /// <param name="entityId">The entity ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of audit logs</returns>
    Task<PagedResult<AuditLog>> GetEntityAuditHistoryAsync(
        string entityType,
        string entityId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of audit logs</returns>
    Task<PagedResult<AuditLog>> GetUserAuditHistoryAsync(
        UserId userId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific warehouse
    /// </summary>
    /// <param name="warehouseId">The warehouse ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of audit logs</returns>
    Task<PagedResult<AuditLog>> GetWarehouseAuditHistoryAsync(
        WarehouseId warehouseId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific variant
    /// </summary>
    /// <param name="variantId">The variant ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of audit logs</returns>
    Task<PagedResult<AuditLog>> GetVariantAuditHistoryAsync(
        VariantId variantId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Filter criteria for audit log queries
/// </summary>
public class AuditLogFilter
{
    /// <summary>
    /// Filter by action type
    /// </summary>
    public AuditAction? Action { get; set; }

    /// <summary>
    /// Filter by entity type
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Filter by entity ID
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Filter by actor (user) ID
    /// </summary>
    public UserId? ActorId { get; set; }

    /// <summary>
    /// Filter by warehouse ID
    /// </summary>
    public WarehouseId? WarehouseId { get; set; }

    /// <summary>
    /// Filter by variant ID
    /// </summary>
    public VariantId? VariantId { get; set; }

    /// <summary>
    /// Filter by start date (inclusive)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter by end date (inclusive)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Search term for description or reason
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "TimestampUtc";

    /// <summary>
    /// Sort direction (true for ascending, false for descending)
    /// </summary>
    public bool SortAscending { get; set; } = false;
}