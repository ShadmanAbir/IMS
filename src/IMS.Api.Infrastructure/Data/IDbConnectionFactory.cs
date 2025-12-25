using System.Data;
using Npgsql;

namespace IMS.Api.Infrastructure.Data;

/// <summary>
/// Interface for creating database connections
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates a new database connection
    /// </summary>
    /// <returns>A new database connection</returns>
    IDbConnection CreateConnection();
    
    /// <summary>
    /// Creates and opens a new database connection asynchronously
    /// </summary>
    /// <returns>A new opened database connection</returns>
    Task<IDbConnection> CreateConnectionAsync();
}

/// <summary>
/// PostgreSQL connection factory for creating Npgsql connections
/// Replaces the previous SQL Server connection factory
/// </summary>
public class PostgreSqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the PostgreSQL connection factory
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <exception cref="ArgumentNullException">Thrown when connectionString is null</exception>
    public PostgreSqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Creates a new PostgreSQL connection
    /// </summary>
    /// <returns>A new NpgsqlConnection instance</returns>
    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    /// <summary>
    /// Creates and opens a new PostgreSQL connection asynchronously
    /// </summary>
    /// <returns>A new opened NpgsqlConnection instance</returns>
    public async Task<IDbConnection> CreateConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}

/// <summary>
/// Legacy SQL Server connection factory - kept for reference but should not be used
/// Use PostgreSqlConnectionFactory instead
/// </summary>
[Obsolete("Use PostgreSqlConnectionFactory instead. SQL Server support has been removed.")]
public class SqlServerConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlServerConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IDbConnection CreateConnection()
    {
        throw new NotSupportedException("SQL Server is no longer supported. Use PostgreSqlConnectionFactory instead.");
    }

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        throw new NotSupportedException("SQL Server is no longer supported. Use PostgreSqlConnectionFactory instead.");
    }
}