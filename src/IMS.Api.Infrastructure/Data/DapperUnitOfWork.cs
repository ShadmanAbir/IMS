using IMS.Api.Application.Common.Interfaces;
using System.Data;

namespace IMS.Api.Infrastructure.Data;

public class DapperUnitOfWork : IUnitOfWork, IDisposable
{
    private readonly IDbConnectionFactory _connectionFactory;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed = false;

    public DapperUnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // With Dapper, changes are committed immediately or through transactions
        // This is mainly for compatibility with the existing interface
        return await Task.FromResult(0);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection == null)
        {
            _connection = await _connectionFactory.CreateConnectionAsync();
        }

        if (_transaction == null)
        {
            _transaction = _connection.BeginTransaction();
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            _transaction.Commit();
            _transaction.Dispose();
            _transaction = null;
        }

        await Task.CompletedTask;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;
        }

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}