namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Interface for write operations using EF Core
/// Provides create, update, delete operations with strong consistency
/// </summary>
/// <typeparam name="TEntity">Domain entity type</typeparam>
/// <typeparam name="TId">Entity identifier type</typeparam>
public interface IWriteRepository<TEntity, TId>
    where TEntity : class
    where TId : class
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}