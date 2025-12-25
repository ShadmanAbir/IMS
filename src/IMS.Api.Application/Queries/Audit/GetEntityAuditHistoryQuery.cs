using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using MediatR;

namespace IMS.Api.Application.Queries.Audit;

/// <summary>
/// Query to get audit history for a specific entity
/// </summary>
public class GetEntityAuditHistoryQuery : IRequest<PagedResult<AuditLogDto>>
{
    /// <summary>
    /// Gets or sets the entity type
    /// </summary>
    public string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the entity ID
    /// </summary>
    public string EntityId { get; set; }

    /// <summary>
    /// Gets or sets the page number
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    public int PageSize { get; set; } = 50;

    public GetEntityAuditHistoryQuery(string entityType, string entityId)
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

/// <summary>
/// Handler for GetEntityAuditHistoryQuery
/// </summary>
public class GetEntityAuditHistoryQueryHandler : IRequestHandler<GetEntityAuditHistoryQuery, PagedResult<AuditLogDto>>
{
    private readonly IAuditRepository _auditRepository;

    public GetEntityAuditHistoryQueryHandler(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<PagedResult<AuditLogDto>> Handle(GetEntityAuditHistoryQuery request, CancellationToken cancellationToken)
    {
        var auditLogs = await _auditRepository.GetEntityAuditHistoryAsync(
            request.EntityType,
            request.EntityId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

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