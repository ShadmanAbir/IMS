using Microsoft.AspNetCore.Authorization;

namespace IMS.Api.Presentation.Authorization;

/// <summary>
/// Authorization attribute for permission-based access control
/// </summary>
public class AuthorizePermissionAttribute : AuthorizeAttribute
{
    public AuthorizePermissionAttribute(string permission)
    {
        Policy = $"Permission:{permission}";
    }
}

/// <summary>
/// Authorization attribute for tenant-aware access control
/// </summary>
public class AuthorizeTenantAttribute : AuthorizeAttribute
{
    public AuthorizeTenantAttribute()
    {
        Policy = "RequireTenantAccess";
    }
}

/// <summary>
/// Authorization attribute for role-based access control with tenant awareness
/// </summary>
public class AuthorizeRoleAttribute : AuthorizeAttribute
{
    public AuthorizeRoleAttribute(params string[] roles)
    {
        Roles = string.Join(",", roles);
        Policy = "RequireTenantAccess"; // Also require tenant access
    }
}