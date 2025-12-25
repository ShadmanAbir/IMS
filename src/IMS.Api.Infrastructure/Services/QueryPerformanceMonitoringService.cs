using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace IMS.Api.Infrastructure.Services;

/// <summary>
/// Service for monitoring and analyzing query performance
/// Tracks slow queries, execution times, and provides performance insights
/// </summary>
public class QueryPerformanceMonitoringService : IQueryPerformanceMonitoringService
{
    private readonly ILogger<QueryPerformanceMonitoringService> _logger;
    private readonly QueryPerformanceOptions _options;
    private readonly ConcurrentDictionary<string, QueryMetrics> _queryMetrics;
    private readonly ConcurrentQueue<SlowQueryLog> _slowQueries;

    public QueryPerformanceMonitoringService(
        ILogger<QueryPerformanceMonitoringService> logger,
        IOptions<QueryPerformanceOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new QueryPerformanceOptions();
        _queryMetrics = new ConcurrentDictionary<string, QueryMetrics>();
        _slowQueries = new ConcurrentQueue<SlowQueryLog>();
    }

    public IDisposable StartQueryMonitoring(string queryName, string? queryText = null, object? parameters = null)
    {
        return new QueryMonitor(this, queryName, queryText, parameters);
    }

    public void RecordQueryExecution(string queryName, TimeSpan executionTime, string? queryText = null, 
        object? parameters = null, Exception? exception = null)
    {
        // Update metrics
        _queryMetrics.AddOrUpdate(queryName, 
            new QueryMetrics(queryName, executionTime),
            (key, existing) => existing.AddExecution(executionTime));

        // Log slow queries
        if (executionTime.TotalMilliseconds > _options.SlowQueryThresholdMs)
        {
            var slowQuery = new SlowQueryLog
            {
                QueryName = queryName,
                ExecutionTime = executionTime,
                QueryText = queryText,
                Parameters = parameters?.ToString(),
                Exception = exception?.Message,
                Timestamp = DateTime.UtcNow
            };

            _slowQueries.Enqueue(slowQuery);

            // Keep only recent slow queries
            while (_slowQueries.Count > _options.MaxSlowQueryLogSize)
            {
                _slowQueries.TryDequeue(out _);
            }

            _logger.LogWarning("Slow query detected: {QueryName} took {ExecutionTimeMs}ms", 
                queryName, executionTime.TotalMilliseconds);
        }

        // Log errors
        if (exception != null)
        {
            _logger.LogError(exception, "Query execution failed: {QueryName}", queryName);
        }
    }

    public QueryPerformanceReport GetPerformanceReport()
    {
        var metrics = _queryMetrics.Values.ToList();
        var slowQueries = _slowQueries.ToList();

        return new QueryPerformanceReport
        {
            GeneratedAt = DateTime.UtcNow,
            TotalQueries = metrics.Sum(m => m.ExecutionCount),
            AverageExecutionTime = metrics.Any() ? 
                TimeSpan.FromMilliseconds(metrics.Average(m => m.AverageExecutionTime.TotalMilliseconds)) : 
                TimeSpan.Zero,
            SlowQueryCount = slowQueries.Count,
            QueryMetrics = metrics.OrderByDescending(m => m.AverageExecutionTime).ToList(),
            RecentSlowQueries = slowQueries.OrderByDescending(q => q.Timestamp).Take(50).ToList(),
            TopSlowQueries = metrics
                .Where(m => m.AverageExecutionTime.TotalMilliseconds > _options.SlowQueryThresholdMs)
                .OrderByDescending(m => m.AverageExecutionTime)
                .Take(20)
                .ToList()
        };
    }

    public void ResetMetrics()
    {
        _queryMetrics.Clear();
        while (_slowQueries.TryDequeue(out _)) { }
        
        _logger.LogInformation("Query performance metrics reset");
    }

    private class QueryMonitor : IDisposable
    {
        private readonly QueryPerformanceMonitoringService _service;
        private readonly string _queryName;
        private readonly string? _queryText;
        private readonly object? _parameters;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public QueryMonitor(QueryPerformanceMonitoringService service, string queryName, 
            string? queryText, object? parameters)
        {
            _service = service;
            _queryName = queryName;
            _queryText = queryText;
            _parameters = parameters;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                _service.RecordQueryExecution(_queryName, _stopwatch.Elapsed, _queryText, _parameters);
                _disposed = true;
            }
        }
    }
}

/// <summary>
/// Interface for query performance monitoring
/// </summary>
public interface IQueryPerformanceMonitoringService
{
    IDisposable StartQueryMonitoring(string queryName, string? queryText = null, object? parameters = null);
    void RecordQueryExecution(string queryName, TimeSpan executionTime, string? queryText = null, 
        object? parameters = null, Exception? exception = null);
    QueryPerformanceReport GetPerformanceReport();
    void ResetMetrics();
}

/// <summary>
/// Configuration options for query performance monitoring
/// </summary>
public class QueryPerformanceOptions
{
    public double SlowQueryThresholdMs { get; set; } = 1000; // 1 second
    public int MaxSlowQueryLogSize { get; set; } = 1000;
    public bool EnableDetailedLogging { get; set; } = true;
    public bool LogQueryParameters { get; set; } = false; // Security consideration
}

/// <summary>
/// Metrics for a specific query
/// </summary>
public class QueryMetrics
{
    private readonly object _lock = new object();
    private long _totalExecutionTime;
    private long _executionCount;
    private long _minExecutionTime = long.MaxValue;
    private long _maxExecutionTime;

    public QueryMetrics(string queryName, TimeSpan initialExecutionTime)
    {
        QueryName = queryName;
        AddExecution(initialExecutionTime);
    }

    public string QueryName { get; }
    public long ExecutionCount => _executionCount;
    public TimeSpan AverageExecutionTime => _executionCount > 0 ? 
        TimeSpan.FromTicks(_totalExecutionTime / _executionCount) : TimeSpan.Zero;
    public TimeSpan MinExecutionTime => TimeSpan.FromTicks(_minExecutionTime);
    public TimeSpan MaxExecutionTime => TimeSpan.FromTicks(_maxExecutionTime);
    public DateTime LastExecuted { get; private set; }

    public QueryMetrics AddExecution(TimeSpan executionTime)
    {
        lock (_lock)
        {
            var ticks = executionTime.Ticks;
            _totalExecutionTime += ticks;
            _executionCount++;
            _minExecutionTime = Math.Min(_minExecutionTime, ticks);
            _maxExecutionTime = Math.Max(_maxExecutionTime, ticks);
            LastExecuted = DateTime.UtcNow;
        }
        return this;
    }
}

/// <summary>
/// Log entry for slow queries
/// </summary>
public class SlowQueryLog
{
    public string QueryName { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
    public string? QueryText { get; set; }
    public string? Parameters { get; set; }
    public string? Exception { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Performance report containing query metrics and analysis
/// </summary>
public class QueryPerformanceReport
{
    public DateTime GeneratedAt { get; set; }
    public long TotalQueries { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public int SlowQueryCount { get; set; }
    public List<QueryMetrics> QueryMetrics { get; set; } = new();
    public List<SlowQueryLog> RecentSlowQueries { get; set; } = new();
    public List<QueryMetrics> TopSlowQueries { get; set; } = new();
}