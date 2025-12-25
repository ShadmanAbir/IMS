using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Infrastructure.Services;

/// <summary>
/// Implementation of tenant context that extracts tenant information from HTTP context
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private TenantId? _overrideTenantId;
    private UserId? _overrideUserId;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public TenantId? CurrentTenantId
    {
        get
        {
            // Return override if set (for system operations)
            if (_overrideTenantId != null)
                return _overrideTenantId;

            // Extract from JWT claims
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
            if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                return TenantId.Create(tenantId);
            }

            return null;
        }
    }

    public UserId? CurrentUserId
    {
        get
        {
            // Return override if set (for system operations)
            if (_overrideUserId != null)
                return _overrideUserId;

            // Extract from JWT claims
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("user_id")?.Value 
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
            {
                return UserId.Create(userId);
            }

            return null;
        }
    }

    public void SetTenantContext(TenantId tenantId, UserId? userId = null)
    {
        _overrideTenantId = tenantId;
        _overrideUserId = userId;
    }
}