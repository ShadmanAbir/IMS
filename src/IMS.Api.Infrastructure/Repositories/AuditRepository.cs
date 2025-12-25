using Dapper;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Text;

namespace IMS.Api.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for audit log operations using Dapper for optimized queries
/// </summary>
public class AuditRepository : IAuditRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AuditRepository> _logger;

    public AuditRepository(
        IDbConnectionFactory connectionFactory,
        ITenantContext tenantContext,
        ILogger<AuditRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO AuditLogs (
                Id, Action, EntityType, EntityId, ActorId, TenantId, TimestampUtc,
                Description, OldValues, NewValues, Context_IpAddress, Context_UserAgent,
                Context_CorrelationId, Context_AdditionalData, WarehouseId, VariantId, Reason
            ) VALUES (
                @Id, @Action, @EntityType, @EntityId, @ActorId, @TenantId, @TimestampUtc,
                @Description, @OldValues, @NewValues, @ContextIpAddress, @ContextUserAgent,
                @ContextCorrelationId, @ContextAdditionalData, @WarehouseId, @VariantId, @Reason
            )";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var parameters = new
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
            ContextIpAddress = auditLog.Context.IpAddress,
            ContextUserAgent = auditLog.Context.UserAgent,
            ContextCorrelationId = auditLog.Context.CorrelationId,
            ContextAdditionalData = auditLog.Context.AdditionalData,
            WarehouseId = auditLog.WarehouseId?.Value,
            VariantId = auditLog.VariantId?.Value,
            Reason = auditLog.Reason
        };

        await connection.ExecuteAsync(sql, parameters);
    }

    public async Task<PagedResult<AuditLog>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        var whereClause = BuildWhereClause(filter);
        var orderClause = BuildOrderClause(filter.SortBy, filter.SortAscending);
        
        var countSql = $@"
            SELECT COUNT(*)
            FROM AuditLogs al
            {whereClause}";

        var dataSql = $@"
            SELECT 
                al.Id, al.Action, al.EntityType, al.EntityId, al.ActorId, al.TenantId,
                al.TimestampUtc, al.Description, al.OldValues, al.NewValues,
                al.Context_IpAddress, al.Context_UserAgent, al.Context_CorrelationId, al.Context_AdditionalData,
                al.WarehouseId, al.VariantId, al.Reason
            FROM AuditLogs al
            {whereClause}
            {orderClause}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var parameters = BuildParameters(filter);
        parameters.Add("Offset", (filter.PageNumber - 1) * filter.PageSize);
        parameters.Add("PageSize", filter.PageSize);

        var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);
        var auditLogs = await connection.QueryAsync<AuditLogDto>(dataSql, parameters);

        var mappedAuditLogs = auditLogs.Select(MapToAuditLog).ToList();

        return new PagedResult<AuditLog>(
            mappedAuditLogs,
            totalCount,
            filter.PageNumber,
            filter.PageSize
        );
    }

    public async Task<PagedResult<AuditLog>> GetEntityAuditHistoryAsync(
        string entityType,
        string entityId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var filter = new AuditLogFilter
        {
            EntityType = entityType,
            EntityId = entityId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return await GetAuditLogsAsync(filter, cancellationToken);
    }

    public async Task<PagedResult<AuditLog>> GetUserAuditHistoryAsync(
        UserId userId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var filter = new AuditLogFilter
        {
            ActorId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return await GetAuditLogsAsync(filter, cancellationToken);
    }

    public async Task<PagedResult<AuditLog>> GetWarehouseAuditHistoryAsync(
        WarehouseId warehouseId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var filter = new AuditLogFilter
        {
            WarehouseId = warehouseId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return await GetAuditLogsAsync(filter, cancellationToken);
    }

    public async Task<PagedResult<AuditLog>> GetVariantAuditHistoryAsync(
        VariantId variantId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var filter = new AuditLogFilter
        {
            VariantId = variantId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return await GetAuditLogsAsync(filter, cancellationToken);
    }

    private string BuildWhereClause(AuditLogFilter filter)
    {
        var conditions = new List<string>();

        // Always filter by tenant
        if (_tenantContext.CurrentTenantId != null)
        {
            conditions.Add("al.TenantId = @TenantId");
        }

        if (filter.Action.HasValue)
            conditions.Add("al.Action = @Action");

        if (!string.IsNullOrEmpty(filter.EntityType))
            conditions.Add("al.EntityType = @EntityType");

        if (!string.IsNullOrEmpty(filter.EntityId))
            conditions.Add("al.EntityId = @EntityId");

        if (filter.ActorId != null)
            conditions.Add("al.ActorId = @ActorId");

        if (filter.WarehouseId != null)
            conditions.Add("al.WarehouseId = @WarehouseId");

        if (filter.VariantId != null)
            conditions.Add("al.VariantId = @VariantId");

        if (filter.StartDate.HasValue)
            conditions.Add("al.TimestampUtc >= @StartDate");

        if (filter.EndDate.HasValue)
            conditions.Add("al.TimestampUtc <= @EndDate");

        if (!string.IsNullOrEmpty(filter.SearchTerm))
            conditions.Add("(al.Description ILIKE @SearchTerm OR al.Reason ILIKE @SearchTerm)");

        return conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";
    }

    private string BuildOrderClause(string sortBy, bool sortAscending)
    {
        var validSortFields = new[] { "TimestampUtc", "Action", "EntityType", "ActorId" };
        var sortField = validSortFields.Contains(sortBy) ? sortBy : "TimestampUtc";
        var sortDirection = sortAscending ? "ASC" : "DESC";
        
        return $"ORDER BY al.{sortField} {sortDirection}";
    }

    private DynamicParameters BuildParameters(AuditLogFilter filter)
    {
        var parameters = new DynamicParameters();

        if (_tenantContext.CurrentTenantId != null)
            parameters.Add("TenantId", _tenantContext.CurrentTenantId.Value);

        if (filter.Action.HasValue)
            parameters.Add("Action", filter.Action.Value.ToString());

        if (!string.IsNullOrEmpty(filter.EntityType))
            parameters.Add("EntityType", filter.EntityType);

        if (!string.IsNullOrEmpty(filter.EntityId))
            parameters.Add("EntityId", filter.EntityId);

        if (filter.ActorId != null)
            parameters.Add("ActorId", filter.ActorId.Value);

        if (filter.WarehouseId != null)
            parameters.Add("WarehouseId", filter.WarehouseId.Value);

        if (filter.VariantId != null)
            parameters.Add("VariantId", filter.VariantId.Value);

        if (filter.StartDate.HasValue)
            parameters.Add("StartDate", filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            parameters.Add("EndDate", filter.EndDate.Value);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
            parameters.Add("SearchTerm", $"%{filter.SearchTerm}%");

        return parameters;
    }

    private AuditLog MapToAuditLog(AuditLogDto dto)
    {
        var context = AuditContext.Create(
            dto.Context_IpAddress,
            dto.Context_UserAgent,
            dto.Context_CorrelationId);

        // Use reflection to create AuditLog since constructor is private
        var auditLog = (AuditLog)Activator.CreateInstance(
            typeof(AuditLog),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new object[]
            {
                AuditLogId.Create(dto.Id),
                Enum.Parse<AuditAction>(dto.Action),
                dto.EntityType,
                dto.EntityId,
                UserId.Create(dto.ActorId),
                TenantId.Create(dto.TenantId),
                dto.Description,
                dto.OldValues,
                dto.NewValues,
                context,
                dto.WarehouseId.HasValue ? WarehouseId.Create(dto.WarehouseId.Value) : null,
                dto.VariantId.HasValue ? VariantId.Create(dto.VariantId.Value) : null,
                dto.Reason
            },
            null);

        // Set the timestamp using reflection since it's set in constructor
        var timestampProperty = typeof(AuditLog).GetProperty("TimestampUtc");
        timestampProperty?.SetValue(auditLog, dto.TimestampUtc);

        return auditLog;
    }

    private class AuditLogDto
    {
        public Guid Id { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public Guid ActorId { get; set; }
        public Guid TenantId { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string Description { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public string Context_IpAddress { get; set; }
        public string Context_UserAgent { get; set; }
        public string Context_CorrelationId { get; set; }
        public string Context_AdditionalData { get; set; }
        public Guid? WarehouseId { get; set; }
        public Guid? VariantId { get; set; }
        public string Reason { get; set; }
    }
}