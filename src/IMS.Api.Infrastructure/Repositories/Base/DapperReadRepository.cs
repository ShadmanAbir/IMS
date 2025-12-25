using AutoMapper;
using Dapper;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Infrastructure.Data;
using System.Data;

namespace IMS.Api.Infrastructure.Repositories.Base;

/// <summary>
/// Base repository for read operations using Dapper with AutoMapper
/// Provides optimized queries for complex dashboard and reporting scenarios
/// </summary>
/// <typeparam name="TDto">Data transfer object type</typeparam>
/// <typeparam name="TEntity">Domain entity type</typeparam>
public abstract class DapperReadRepository<TDto, TEntity>
    where TDto : class
    where TEntity : class
{
    protected readonly IDbConnectionFactory ConnectionFactory;
    protected readonly IMapper Mapper;
    protected readonly ITenantContext TenantContext;

    protected DapperReadRepository(
        IDbConnectionFactory connectionFactory, 
        IMapper mapper, 
        ITenantContext tenantContext)
    {
        ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        TenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    /// <summary>
    /// Executes a query and maps the results to domain entities
    /// </summary>
    protected async Task<List<TEntity>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        where T : class
    {
        using var connection = await ConnectionFactory.CreateConnectionAsync();
        var dtos = await connection.QueryAsync<T>(sql, parameters);
        return Mapper.Map<List<TEntity>>(dtos);
    }

    /// <summary>
    /// Executes a query and returns a single domain entity
    /// </summary>
    protected async Task<TEntity?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        where T : class
    {
        using var connection = await ConnectionFactory.CreateConnectionAsync();
        var dto = await connection.QuerySingleOrDefaultAsync<T>(sql, parameters);
        return dto != null ? Mapper.Map<TEntity>(dto) : null;
    }

    /// <summary>
    /// Executes a query and returns DTOs directly (for dashboard/reporting scenarios)
    /// </summary>
    protected async Task<List<T>> QueryDtosAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        where T : class
    {
        using var connection = await ConnectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<T>(sql, parameters);
        return results.ToList();
    }

    /// <summary>
    /// Executes a scalar query (count, sum, etc.)
    /// </summary>
    protected async Task<T> QueryScalarAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        using var connection = await ConnectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleAsync<T>(sql, parameters);
    }

    /// <summary>
    /// Executes multiple queries in a single round trip
    /// </summary>
    protected async Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var connection = await ConnectionFactory.CreateConnectionAsync();
        return await connection.QueryMultipleAsync(sql, parameters);
    }

    /// <summary>
    /// Adds tenant filter to WHERE clause if tenant context is available
    /// </summary>
    protected string AddTenantFilter(string baseWhere, string tenantColumn = "TenantId")
    {
        if (TenantContext.CurrentTenantId == null)
            return baseWhere;

        var tenantFilter = $"{tenantColumn} = @TenantId";
        
        if (string.IsNullOrWhiteSpace(baseWhere))
            return $"WHERE {tenantFilter}";
        
        return baseWhere.Contains("WHERE", StringComparison.OrdinalIgnoreCase)
            ? $"{baseWhere} AND {tenantFilter}"
            : $"WHERE {tenantFilter} AND ({baseWhere})";
    }

    /// <summary>
    /// Gets tenant parameter for queries
    /// </summary>
    protected DynamicParameters GetTenantParameters(object? additionalParams = null)
    {
        var parameters = new DynamicParameters(additionalParams);
        
        if (TenantContext.CurrentTenantId != null)
        {
            parameters.Add("TenantId", TenantContext.CurrentTenantId.Value);
        }
        
        return parameters;
    }
}