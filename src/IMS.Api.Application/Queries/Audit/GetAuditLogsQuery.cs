using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;
using MediatR;

namespace IMS.Api.Application.Queries.Audit;

/// <summary>
/// Query to get audit logs with filtering and pagination
/// </summary>
public class GetAuditLogsQuery : IRequest<PagedResult<AuditLogDto>>
{
    /// <summary>
    /// Gets or sets the action filter
    /// </summary>
    public AuditAction? Action { get; set; }

    /// <summary>
    /// Gets or sets the entity type filter
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the entity ID filter
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the actor ID filter
    /// </summary>
    public Guid? ActorId { get; set; }

    /// <summary>
    /// Gets or sets the warehouse ID filter
    /// </summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>
    /// Gets or sets the variant ID filter
    /// </summary>
    public Guid? VariantId { get; set; }

    /// <summary>
    /// Gets or sets the start date filter
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date filter
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the search term
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the page number
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the sort field
    /// </summary>
    public string SortBy { get; set; } = "TimestampUtc";

    /// <summary>
    /// Gets or sets the sort direction
    /// </summary>
    public bool SortAscending { get; set; } = false;
}

/// <summary>
/// Handler for GetAuditLogsQuery
/// </summary>
public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IAuditRepository _auditRepository;

    public GetAuditLogsQueryHandler(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var filter = new AuditLogFilter
        {
            Action = request.Action,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            ActorId = request.ActorId.HasValue ? UserId.Create(request.ActorId.Value) : null,
            WarehouseId = request.WarehouseId.HasValue ? WarehouseId.Create(request.WarehouseId.Value) : null,
            VariantId = request.VariantId.HasValue ? VariantId.Create(request.VariantId.Value) : null,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SearchTerm = request.SearchTerm,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortBy = request.SortBy,
            SortAscending = request.SortAscending
        };

        var auditLogs = await _auditRepository.GetAuditLogsAsync(filter, cancellationToken);

        var auditLogDtos = auditLogs.Items.Select(MapToDto).ToList();

        return new PagedResult<AuditLogDto>(
            auditLogDtos,
            auditLogs.TotalCount,
            auditLogs.PageNumber,
            auditLogs.PageSize);
    }

    private AuditLogDto MapToDto(Domain.Entities.AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id.Value,
            Action = auditLog.Action.ToString(),
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            ActorId = auditLog.ActorId.Value,
            TenantId = auditLog.TenantId.Value,
            TimestampUtc = auditLog.TimestampUtc,
            Description = auditLog.Description,
            OldValues = auditLog.OldValues,
            NewValues = auditLog.NewValues,
            IpAddress = auditLog.Context.IpAddress,
            UserAgent = auditLog.Context.UserAgent,
            CorrelationId = auditLog.Context.CorrelationId,
            AdditionalData = auditLog.Context.AdditionalData,
            WarehouseId = auditLog.WarehouseId?.Value,
            VariantId = auditLog.VariantId?.Value,
            Reason = auditLog.Reason
        };
    }
}