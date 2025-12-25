using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IMS.Api.Application.Common.Behaviors;

/// <summary>
/// MediatR behavior that automatically logs audit entries for commands and queries
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

    public AuditBehavior(IAuditService auditService, ILogger<AuditBehavior<TRequest, TResponse>> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var isCommand = requestName.EndsWith("Command");
        var isQuery = requestName.EndsWith("Query");

        // Only audit commands and specific queries
        if (!isCommand && !ShouldAuditQuery(requestName))
        {
            return await next();
        }

        var startTime = DateTime.UtcNow;
        Exception exception = null;
        TResponse response = default;

        try
        {
            response = await next();
            return response;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            // Log the audit entry
            await LogAuditEntry(request, response, requestName, isCommand, startTime, exception, cancellationToken);
        }
    }

    private async Task LogAuditEntry(
        TRequest request,
        TResponse response,
        string requestName,
        bool isCommand,
        DateTime startTime,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            var duration = DateTime.UtcNow - startTime;
            var action = DetermineAuditAction(requestName, isCommand);
            var description = CreateDescription(requestName, isCommand, duration, exception);

            var requestData = SerializeRequest(request);
            var responseData = exception == null ? SerializeResponse(response) : null;
            var errorData = exception != null ? SerializeException(exception) : null;

            // Combine response and error data
            var auditData = new Dictionary<string, object>
            {
                ["Duration"] = duration.TotalMilliseconds,
                ["Success"] = exception == null
            };

            if (responseData != null)
                auditData["Response"] = responseData;

            if (errorData != null)
                auditData["Error"] = errorData;

            await _auditService.LogAsync(
                action,
                "Command/Query",
                requestName,
                description,
                requestData,
                auditData,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry for {RequestName}", requestName);
        }
    }

    private AuditAction DetermineAuditAction(string requestName, bool isCommand)
    {
        if (!isCommand)
            return AuditAction.DataExport; // Queries are considered data exports

        // Map command names to audit actions
        return requestName.ToLowerInvariant() switch
        {
            var name when name.Contains("create") => AuditAction.Create,
            var name when name.Contains("update") || name.Contains("modify") => AuditAction.Update,
            var name when name.Contains("delete") || name.Contains("remove") => AuditAction.Delete,
            var name when name.Contains("stockmovement") || name.Contains("purchase") || name.Contains("sale") => AuditAction.StockMovement,
            var name when name.Contains("reservation") && name.Contains("create") => AuditAction.ReservationCreate,
            var name when name.Contains("reservation") && name.Contains("modify") => AuditAction.ReservationModify,
            var name when name.Contains("reservation") && name.Contains("cancel") => AuditAction.ReservationCancel,
            var name when name.Contains("import") => AuditAction.DataImport,
            var name when name.Contains("export") => AuditAction.DataExport,
            _ => AuditAction.Update // Default for other commands
        };
    }

    private string CreateDescription(string requestName, bool isCommand, TimeSpan duration, Exception exception)
    {
        var operationType = isCommand ? "Command" : "Query";
        var status = exception == null ? "completed" : "failed";
        
        return $"{operationType} '{requestName}' {status} in {duration.TotalMilliseconds:F2}ms";
    }

    private bool ShouldAuditQuery(string requestName)
    {
        // Only audit specific sensitive queries
        var sensitiveQueries = new[]
        {
            "GetAuditLogsQuery",
            "GetUserAuditHistoryQuery",
            "GetStockMovementHistoryQuery",
            "GetBulkInventoryLevelsQuery"
        };

        return sensitiveQueries.Any(sq => requestName.Contains(sq));
    }

    private object SerializeRequest(TRequest request)
    {
        try
        {
            // Create a sanitized version of the request (remove sensitive data)
            var requestType = request.GetType();
            var properties = requestType.GetProperties()
                .Where(p => !IsSensitiveProperty(p.Name))
                .ToDictionary(
                    p => p.Name,
                    p => p.GetValue(request));

            return properties;
        }
        catch
        {
            return new { RequestType = typeof(TRequest).Name };
        }
    }

    private object SerializeResponse(TResponse response)
    {
        try
        {
            if (response == null)
                return null;

            // For simple types, return as-is
            if (response.GetType().IsPrimitive || response is string)
                return response;

            // For complex types, create a summary
            var responseType = response.GetType();
            
            // If it's a collection, return count
            if (response is System.Collections.IEnumerable enumerable && !(response is string))
            {
                var count = 0;
                foreach (var item in enumerable)
                    count++;
                
                return new { Count = count, Type = responseType.Name };
            }

            // For other complex types, return type info
            return new { Type = responseType.Name };
        }
        catch
        {
            return new { ResponseType = typeof(TResponse).Name };
        }
    }

    private object SerializeException(Exception exception)
    {
        return new
        {
            Type = exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace?.Split('\n').Take(5).ToArray() // Limit stack trace
        };
    }

    private bool IsSensitiveProperty(string propertyName)
    {
        var sensitiveProperties = new[]
        {
            "password", "token", "secret", "key", "credential",
            "authorization", "authentication", "bearer"
        };

        return sensitiveProperties.Any(sp => 
            propertyName.ToLowerInvariant().Contains(sp));
    }
}