using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using IMS.Api.Application.Common.Interfaces;
using System.Security.Claims;

namespace IMS.Api.Infrastructure.Authorization;

/// <summary>
/// Authorization handler that ensures users can only access resources within their tenant
/// </summary>
public class TenantAuthorizationHandler : AuthorizationHandler<TenantAccessRequirement>
{
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantAuthorizationHandler(
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantAccessRequirement requirement)
    {
        // System administrators can access all tenants
        if (context.User.IsInRole("SystemAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Get tenant ID from JWT claims
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        if (tenantIdClaim == null || !Guid.TryParse(tenantIdClaim, out var userTenantId))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Get current tenant context
        var currentTenantId = _tenantContext.CurrentTenantId;
        if (currentTenantId == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Ensure user's tenant matches the current context
        if (userTenantId == currentTenantId.Value)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Authorization requirement for tenant access
/// </summary>
public class TenantAccessRequirement : IAuthorizationRequirement
{
    public TenantAccessRequirement()
    {
    }
}

/// <summary>
/// Authorization handler for permission-based access control
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // System administrators have all permissions
        if (context.User.IsInRole("SystemAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if user has the required permission claim
        var hasPermission = context.User.HasClaim("permission", requirement.Permission);
        if (hasPermission)
        {
            context.Succeed(requirement);
        }
        else
        {
            // Check role-based permissions as fallback
            var hasRolePermission = CheckRoleBasedPermission(context.User, requirement.Permission);
            if (hasRolePermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        return Task.CompletedTask;
    }

    private static bool CheckRoleBasedPermission(ClaimsPrincipal user, string permission)
    {
        return permission switch
        {
            "manage_inventory" => user.IsInRole("WarehouseManager") || user.IsInRole("TenantAdmin"),
            "view_reports" => user.IsInRole("InventoryAnalyst") || user.IsInRole("WarehouseManager") || user.IsInRole("TenantAdmin"),
            "manage_users" => user.IsInRole("TenantAdmin"),
            "manage_warehouses" => user.IsInRole("WarehouseManager") || user.IsInRole("TenantAdmin"),
            "manage_products" => user.IsInRole("WarehouseManager") || user.IsInRole("TenantAdmin"),
            "view_inventory" => user.IsInRole("WarehouseOperator") || user.IsInRole("InventoryAnalyst") || user.IsInRole("WarehouseManager") || user.IsInRole("TenantAdmin"),
            "record_stock_movements" => user.IsInRole("WarehouseOperator") || user.IsInRole("WarehouseManager") || user.IsInRole("TenantAdmin"),
            "manage_reservations" => user.IsInRole("WarehouseOperator") || user.IsInRole("WarehouseManager") || user.IsInRole("TenantAdmin"),
            _ => false
        };
    }
}

/// <summary>
/// Authorization requirement for permission-based access control
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}