using Microsoft.AspNetCore.Mvc;
using IMS.Api.Application.Common.Models;
using MediatR;

namespace IMS.Api.Presentation.Controllers;

/// <summary>
/// Base controller with common functionality for all API controllers
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    protected readonly IMediator Mediator;

    protected BaseController(IMediator mediator)
    {
        Mediator = mediator;
    }

    /// <summary>
    /// Gets the current user ID from JWT claims
    /// </summary>
    /// <returns>User ID if found, null otherwise</returns>
    protected Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Gets the current tenant ID from JWT claims
    /// </summary>
    /// <returns>Tenant ID if found, null otherwise</returns>
    protected Guid? GetCurrentTenantId()
    {
        var tenantIdClaim = User.FindFirst("tenantId");
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return tenantId;
        }
        return null;
    }

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <param name="message">Error message</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="details">Additional error details</param>
    /// <returns>ObjectResult with error response</returns>
    protected ObjectResult CreateErrorResponse(
        string errorCode, 
        string message, 
        int statusCode = 400, 
        Dictionary<string, object>? details = null)
    {
        var errorResponse = new
        {
            ErrorCode = errorCode,
            Message = message,
            Details = details ?? new Dictionary<string, object>(),
            TimestampUtc = DateTime.UtcNow,
            TraceId = HttpContext.TraceIdentifier
        };

        return StatusCode(statusCode, errorResponse);
    }

    /// <summary>
    /// Creates a standardized success response
    /// </summary>
    /// <typeparam name="T">Type of data</typeparam>
    /// <param name="data">Response data</param>
    /// <param name="message">Success message</param>
    /// <returns>OkObjectResult with success response</returns>
    protected OkObjectResult CreateSuccessResponse<T>(T data, string? message = null)
    {
        var response = ApiResponse<T>.SuccessResponse(data, message);
        response.TraceId = HttpContext.TraceIdentifier;
        return Ok(response);
    }

    /// <summary>
    /// Creates a standardized success response without data
    /// </summary>
    /// <param name="message">Success message</param>
    /// <returns>OkObjectResult with success response</returns>
    protected OkObjectResult CreateSuccessResponse(string? message = null)
    {
        var response = ApiResponse.SuccessResponse(message);
        response.TraceId = HttpContext.TraceIdentifier;
        return Ok(response);
    }

    /// <summary>
    /// Creates a standardized paginated response
    /// </summary>
    /// <typeparam name="T">Type of items</typeparam>
    /// <param name="pagedData">Paged data</param>
    /// <param name="message">Success message</param>
    /// <returns>OkObjectResult with paginated response</returns>
    protected OkObjectResult CreatePaginatedResponse<T>(PagedResult<T> pagedData, string? message = null)
    {
        var response = PaginatedApiResponse<T>.SuccessResponse(pagedData, message);
        response.TraceId = HttpContext.TraceIdentifier;
        return Ok(response);
    }

    /// <summary>
    /// Handles Result<T> responses and converts them to appropriate HTTP responses
    /// </summary>
    /// <typeparam name="T">Type of result data</typeparam>
    /// <param name="result">Result to handle</param>
    /// <param name="successMessage">Message for successful results</param>
    /// <returns>ActionResult based on result status</returns>
    protected ActionResult HandleResult<T>(Result<T> result, string? successMessage = null)
    {
        if (result.IsSuccess)
        {
            return CreateSuccessResponse(result.Value, successMessage);
        }

        return result.ErrorCode switch
        {
            "NOT_FOUND" or "PRODUCT_NOT_FOUND" or "VARIANT_NOT_FOUND" or "WAREHOUSE_NOT_FOUND" or "RESERVATION_NOT_FOUND" or "INVENTORY_NOT_FOUND" or "ORIGINAL_SALE_NOT_FOUND" or "SALE_NOT_FOUND" => 
                CreateErrorResponse(result.ErrorCode, result.ErrorMessage, 404),
            
            "DUPLICATE_SKU" or "DUPLICATE_CODE" or "DUPLICATE_NAME" or "OPENING_BALANCE_EXISTS" => 
                CreateErrorResponse(result.ErrorCode, result.ErrorMessage, 409),
            
            "UNAUTHORIZED" or "INVALID_TOKEN" => 
                CreateErrorResponse(result.ErrorCode, result.ErrorMessage, 401),
            
            "FORBIDDEN" => 
                CreateErrorResponse(result.ErrorCode, result.ErrorMessage, 403),
            
            "VALIDATION_FAILED" or "INVALID_INPUT" or "INVALID_ARGUMENT" or "REQUIRED_FIELD_MISSING" => 
                CreateErrorResponse(result.ErrorCode, result.ErrorMessage, 400),
            
            _ => CreateErrorResponse(result.ErrorCode, result.ErrorMessage, 400)
        };
    }

    /// <summary>
    /// Handles Result responses and converts them to appropriate HTTP responses
    /// </summary>
    /// <param name="result">Result to handle</param>
    /// <param name="successMessage">Message for successful results</param>
    /// <returns>ActionResult based on result status</returns>
    protected ActionResult HandleResult(Result result, string? successMessage = null)
    {
        if (result.IsSuccess)
        {
            return CreateSuccessResponse(successMessage);
        }

        return result.ErrorCode switch
        {
            "NOT_FOUND" or "PRODUCT_NOT_FOUND" or "VARIANT_NOT_FOUND" or "WAREHOUSE_NOT_FOUND" or "RESERVATION_NOT_FOUND" or "INVENTORY_NOT_FOUND" or "ORIGINAL_SALE_NOT_FOUND" or "SALE_NOT_FOUND" => 
                CreateErrorResponse(result.ErrorCode, result.ErrorMessage, 404),
            
            "DUPLICATE_SKU" or "DUPLICATE_CODE" or "DUPLICATE_NAME" or "OPENING_BALANCE_EXISTS" => 
                CreateErrorResponse(result.ErrorCode, result.ErrorMessage, 409),
            
            "UNAUTHORIZED" or "INVALID_TOKEN" => 
                CreateErrorResponse(result.ErrorCode, result.ErrorMessage, 401),
            
            "FORBIDDEN" => 
                CreateErrorResponse(result.ErrorCode, result.ErrorMessage, 403),
            
            "VALIDATION_FAILED" or "INVALID_INPUT" or "INVALID_ARGUMENT" or "REQUIRED_FIELD_MISSING" => 
                CreateErrorResponse(result.ErrorCode, result.ErrorMessage, 400),
            
            _ => CreateErrorResponse(result.ErrorCode, result.ErrorMessage, 400)
        };
    }

    /// <summary>
    /// Validates that the current user is authenticated
    /// </summary>
    /// <returns>ActionResult if validation fails, null if successful</returns>
    protected ActionResult? ValidateAuthentication()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return CreateErrorResponse("INVALID_TOKEN", "User ID not found in token", 401);
        }
        return null;
    }

    /// <summary>
    /// Validates that the current user belongs to a tenant
    /// </summary>
    /// <returns>ActionResult if validation fails, null if successful</returns>
    protected ActionResult? ValidateTenant()
    {
        var tenantId = GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            return CreateErrorResponse("INVALID_TENANT", "Tenant ID not found in token", 401);
        }
        return null;
    }
}