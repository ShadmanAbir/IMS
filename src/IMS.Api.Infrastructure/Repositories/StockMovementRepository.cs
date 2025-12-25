using AutoMapper;
using Dapper;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Models;
using IMS.Api.Application.Queries.Inventory;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Infrastructure.Data;
using IMS.Api.Infrastructure.Data.DTOs;
using IMS.Api.Infrastructure.Repositories.Base;

namespace IMS.Api.Infrastructure.Repositories;

/// <summary>
/// Hybrid stock movement repository implementing EF Core for writes and Dapper for reads
/// Write operations use EF Core with AutoMapper for strong consistency
/// Read operations use Dapper for optimized performance, especially for complex queries
/// </summary>
public class StockMovementRepository : IStockMovementRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenantContext;

    public StockMovementRepository(
        ApplicationDbContext context,
        IDbConnectionFactory connectionFactory, 
        IMapper mapper,
        ITenantContext tenantContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    #region Write Operations (EF Core)

    public async Task AddAsync(StockMovement entity, CancellationToken cancellationToken = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        await _context.StockMovements.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(StockMovement entity, CancellationToken cancellationToken = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        _context.StockMovements.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(StockMovement entity, CancellationToken cancellationToken = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        _context.StockMovements.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Read Operations (Dapper)

    public async Task<StockMovement?> GetByIdAsync(StockMovementId id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter("SELECT * FROM StockMovements WHERE Id = @Id", "sm.TenantId");
        var parameters = GetTenantParameters(new { Id = id.Value });
        
        var dto = await connection.QuerySingleOrDefaultAsync<StockMovementDto>(sql, parameters);
        
        return dto != null ? _mapper.Map<StockMovement>(dto) : null;
    }

    public async Task<IEnumerable<StockMovement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter("SELECT * FROM StockMovements ORDER BY TimestampUtc DESC");
        var parameters = GetTenantParameters();
        
        var dtos = await connection.QueryAsync<StockMovementDto>(sql, parameters);
        
        return _mapper.Map<IEnumerable<StockMovement>>(dtos);
    }

    public async Task<PagedResult<StockMovement>> GetMovementHistoryAsync(StockMovementQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var whereConditions = new List<string>();
        var parameters = GetTenantParameters();

        if (query.VariantId != null)
        {
            whereConditions.Add("ii.VariantId = @VariantId");
            parameters.Add("VariantId", query.VariantId.Value);
        }

        if (query.WarehouseId != null)
        {
            whereConditions.Add("ii.WarehouseId = @WarehouseId");
            parameters.Add("WarehouseId", query.WarehouseId.Value);
        }

        if (query.MovementType != null)
        {
            whereConditions.Add("sm.Type = @MovementType");
            parameters.Add("MovementType", query.MovementType.ToString());
        }

        if (query.ActorId != null)
        {
            whereConditions.Add("sm.ActorId = @ActorId");
            parameters.Add("ActorId", query.ActorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ReferenceNumber))
        {
            whereConditions.Add("sm.ReferenceNumber = @ReferenceNumber");
            parameters.Add("ReferenceNumber", query.ReferenceNumber);
        }

        if (query.StartDate != null)
        {
            whereConditions.Add("sm.TimestampUtc >= @StartDate");
            parameters.Add("StartDate", query.StartDate);
        }

        if (query.EndDate != null)
        {
            whereConditions.Add("sm.TimestampUtc <= @EndDate");
            parameters.Add("EndDate", query.EndDate);
        }

        // Add tenant filter
        if (_tenantContext.CurrentTenantId != null)
        {
            whereConditions.Add("ii.TenantId = @TenantId");
        }

        var whereClause = whereConditions.Any() ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        // Count query
        var countSql = $@"
            SELECT COUNT(1) 
            FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            {whereClause}";

        var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

        // Data query
        parameters.Add("Offset", (query.PageNumber - 1) * query.PageSize);
        parameters.Add("PageSize", query.PageSize);

        var dataSql = $@"
            SELECT sm.* 
            FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            {whereClause}
            ORDER BY sm.TimestampUtc DESC
            LIMIT @PageSize OFFSET @Offset";

        var dtos = await connection.QueryAsync<StockMovementDto>(dataSql, parameters);
        var movements = _mapper.Map<List<StockMovement>>(dtos);

        return new PagedResult<StockMovement>
        {
            Items = movements,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<List<StockMovement>> GetMovementsByReferenceAsync(string referenceNumber, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter(@"
            SELECT sm.* FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            WHERE sm.ReferenceNumber = @ReferenceNumber 
            ORDER BY sm.TimestampUtc DESC", "ii.TenantId");

        var parameters = GetTenantParameters(new { ReferenceNumber = referenceNumber });
        var dtos = await connection.QueryAsync<StockMovementDto>(sql, parameters);
        return _mapper.Map<List<StockMovement>>(dtos);
    }

    public async Task<List<StockMovement>> GetMovementsByInventoryItemAsync(InventoryItemId inventoryItemId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter(@"
            SELECT sm.* FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            WHERE sm.InventoryItemId = @InventoryItemId 
            ORDER BY sm.TimestampUtc DESC", "ii.TenantId");

        var parameters = GetTenantParameters(new { InventoryItemId = inventoryItemId.Value });
        var dtos = await connection.QueryAsync<StockMovementDto>(sql, parameters);
        return _mapper.Map<List<StockMovement>>(dtos);
    }

    public async Task<List<StockMovement>> GetMovementsByVariantAsync(VariantId variantId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter(@"
            SELECT sm.* FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            WHERE ii.VariantId = @VariantId
            ORDER BY sm.TimestampUtc DESC", "ii.TenantId");

        var parameters = GetTenantParameters(new { VariantId = variantId.Value });
        var dtos = await connection.QueryAsync<StockMovementDto>(sql, parameters);
        return _mapper.Map<List<StockMovement>>(dtos);
    }

    public async Task<List<StockMovement>> GetMovementsByTypeAsync(MovementType movementType, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter(@"
            SELECT sm.* FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            WHERE sm.Type = @MovementType", "ii.TenantId");
        
        var parameters = GetTenantParameters(new { MovementType = movementType.ToString() });

        if (startDate != null)
        {
            sql += " AND sm.TimestampUtc >= @StartDate";
            parameters.Add("StartDate", startDate);
        }

        if (endDate != null)
        {
            sql += " AND sm.TimestampUtc <= @EndDate";
            parameters.Add("EndDate", endDate);
        }

        sql += " ORDER BY sm.TimestampUtc DESC";

        var dtos = await connection.QueryAsync<StockMovementDto>(sql, parameters);
        return _mapper.Map<List<StockMovement>>(dtos);
    }

    public async Task<List<StockMovement>> GetRecentMovementsAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = AddTenantFilter(@"
            SELECT sm.* FROM StockMovements sm
            INNER JOIN InventoryItems ii ON sm.InventoryItemId = ii.Id
            ORDER BY sm.TimestampUtc DESC
            LIMIT @Count", "ii.TenantId");

        var parameters = GetTenantParameters(new { Count = count });
        var dtos = await connection.QueryAsync<StockMovementDto>(sql, parameters);
        return _mapper.Map<List<StockMovement>>(dtos);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Adds tenant filter to WHERE clause if tenant context is available
    /// Also adds soft delete filter to exclude deleted records using PostgreSQL syntax
    /// </summary>
    private string AddTenantFilter(string baseQuery, string tenantColumn = "TenantId")
    {
        var filters = new List<string>();
        
        // Add soft delete filter for inventory items using PostgreSQL boolean syntax
        if (baseQuery.Contains("InventoryItems", StringComparison.OrdinalIgnoreCase))
        {
            filters.Add("ii.IsDeleted = false");
        }
        
        // Add tenant filter if available
        if (_tenantContext.CurrentTenantId != null)
        {
            filters.Add($"{tenantColumn} = @TenantId");
        }
        
        if (filters.Count == 0)
            return baseQuery;
        
        var combinedFilter = string.Join(" AND ", filters);
        
        if (baseQuery.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
            return baseQuery.Replace("WHERE", $"WHERE {combinedFilter} AND (") + ")";
        
        return $"{baseQuery} WHERE {combinedFilter}";
    }

    /// <summary>
    /// Gets tenant parameter for queries
    /// </summary>
    private DynamicParameters GetTenantParameters(object? additionalParams = null)
    {
        var parameters = new DynamicParameters(additionalParams);
        
        if (_tenantContext.CurrentTenantId != null)
        {
            parameters.Add("TenantId", _tenantContext.CurrentTenantId.Value);
        }
        
        return parameters;
    }

    #endregion
}