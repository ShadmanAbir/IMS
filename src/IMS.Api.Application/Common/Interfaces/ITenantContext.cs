using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Common.Interfaces;

/// <summary>
/// Interface for accessing current tenant context
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID from the authenticated user context
    /// </summary>
    TenantId? CurrentTenantId { get; }

    /// <summary>
    /// Gets the current user ID from the authenticated user context
    /// </summary>
    UserId? CurrentUserId { get; }

    /// <summary>
    /// Sets the current tenant context (used for system operations)
    /// </summary>
    void SetTenantContext(TenantId tenantId, UserId? userId = null);
}