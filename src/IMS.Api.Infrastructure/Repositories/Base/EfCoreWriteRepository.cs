using AutoMapper;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Domain.Common;
using IMS.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IMS.Api.Infrastructure.Repositories.Base;

/// <summary>
/// Base repository for write operations using EF Core with AutoMapper
/// Provides create, update, delete operations with strong consistency
/// </summary>
/// <typeparam name="TEntity">Domain entity type</typeparam>
/// <typeparam name="TId">Entity identifier type</typeparam>
public abstract class EfCoreWriteRepository<TEntity, TId> : IWriteRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : class
{
    protected readonly ApplicationDbContext Context;
    protected readonly IMapper Mapper;
    protected readonly DbSet<TEntity> DbSet;

    protected EfCoreWriteRepository(ApplicationDbContext context, IMapper mapper)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        DbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        DbSet.Update(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        DbSet.Remove(entity);
        await Task.CompletedTask;
    }

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }
}