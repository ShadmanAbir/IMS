namespace IMS.Api.Application.Common.Models;

/// <summary>
/// Standardized API response wrapper
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object>? Details { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> ErrorResponse(string errorCode, string message, Dictionary<string, object>? details = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            Details = details
        };
    }
}

/// <summary>
/// Standardized API response wrapper without data
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object>? Details { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }

    public static ApiResponse SuccessResponse(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    public static ApiResponse ErrorResponse(string errorCode, string message, Dictionary<string, object>? details = null)
    {
        return new ApiResponse
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            Details = details
        };
    }
}

/// <summary>
/// Paginated API response
/// </summary>
/// <typeparam name="T">The type of items in the collection</typeparam>
public class PaginatedApiResponse<T> : ApiResponse<PagedResult<T>>
{
    public static new PaginatedApiResponse<T> SuccessResponse(PagedResult<T> pagedData, string? message = null)
    {
        return new PaginatedApiResponse<T>
        {
            Success = true,
            Data = pagedData,
            Message = message
        };
    }

    public static new PaginatedApiResponse<T> ErrorResponse(string errorCode, string message, Dictionary<string, object>? details = null)
    {
        return new PaginatedApiResponse<T>
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            Details = details
        };
    }
}