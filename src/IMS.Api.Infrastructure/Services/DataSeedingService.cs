using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IMS.Api.Domain.Entities;
using System.Security.Claims;

namespace IMS.Api.Infrastructure.Services;

/// <summary>
/// Service for seeding initial data including roles and permissions
/// </summary>
public static class DataSeedingService
{
    /// <summary>
    /// Seeds default roles and permissions
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeedingService>>();

        try
        {
            await SeedRolesAsync(roleManager, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding data");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        var roles = new[]
        {
            new { Name = "SystemAdmin", Description = "System administrator with full access to all tenants and system configuration" },
            new { Name = "TenantAdmin", Description = "Tenant administrator with full access within their tenant" },
            new { Name = "WarehouseManager", Description = "Warehouse manager with access to inventory management and reporting" },
            new { Name = "InventoryAnalyst", Description = "Inventory analyst with access to reports and analytics" },
            new { Name = "WarehouseOperator", Description = "Warehouse operator with basic inventory operations access" }
        };

        foreach (var roleInfo in roles)
        {
            var existingRole = await roleManager.FindByNameAsync(roleInfo.Name);
            if (existingRole == null)
            {
                logger.LogInformation("Creating role: {RoleName}", roleInfo.Name);
                
                var role = new ApplicationRole(roleInfo.Name, roleInfo.Description);
                var result = await roleManager.CreateAsync(role);

                if (result.Succeeded)
                {
                    logger.LogInformation("Role created successfully: {RoleName}", roleInfo.Name);
                    
                    // Add role-specific claims/permissions
                    await SeedRoleClaimsAsync(roleManager, role, logger);
                }
                else
                {
                    logger.LogError("Failed to create role {RoleName}: {Errors}", 
                        roleInfo.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Role already exists: {RoleName}", roleInfo.Name);
            }
        }
    }

    private static async Task SeedRoleClaimsAsync(RoleManager<ApplicationRole> roleManager, ApplicationRole role, ILogger logger)
    {
        var roleClaims = GetRolePermissions(role.Name!);

        foreach (var claim in roleClaims)
        {
            var existingClaims = await roleManager.GetClaimsAsync(role);
            if (!existingClaims.Any(c => c.Type == claim.Type && c.Value == claim.Value))
            {
                var result = await roleManager.AddClaimAsync(role, claim);
                if (result.Succeeded)
                {
                    logger.LogInformation("Added claim {ClaimType}:{ClaimValue} to role {RoleName}", 
                        claim.Type, claim.Value, role.Name);
                }
                else
                {
                    logger.LogError("Failed to add claim {ClaimType}:{ClaimValue} to role {RoleName}: {Errors}",
                        claim.Type, claim.Value, role.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private static List<Claim> GetRolePermissions(string roleName)
    {
        return roleName switch
        {
            "SystemAdmin" => new List<Claim>
            {
                new("permission", "manage_system"),
                new("permission", "manage_tenants"),
                new("permission", "manage_users"),
                new("permission", "manage_roles"),
                new("permission", "manage_inventory"),
                new("permission", "view_reports"),
                new("permission", "manage_warehouses"),
                new("permission", "manage_products"),
                new("permission", "manage_pricing"),
                new("permission", "view_audit_logs")
            },
            "TenantAdmin" => new List<Claim>
            {
                new("permission", "manage_users"),
                new("permission", "manage_roles"),
                new("permission", "manage_inventory"),
                new("permission", "view_reports"),
                new("permission", "manage_warehouses"),
                new("permission", "manage_products"),
                new("permission", "manage_pricing"),
                new("permission", "view_audit_logs")
            },
            "WarehouseManager" => new List<Claim>
            {
                new("permission", "manage_inventory"),
                new("permission", "view_reports"),
                new("permission", "manage_warehouses"),
                new("permission", "manage_products"),
                new("permission", "view_stock_movements"),
                new("permission", "manage_reservations")
            },
            "InventoryAnalyst" => new List<Claim>
            {
                new("permission", "view_reports"),
                new("permission", "view_inventory"),
                new("permission", "view_stock_movements"),
                new("permission", "view_reservations"),
                new("permission", "export_data")
            },
            "WarehouseOperator" => new List<Claim>
            {
                new("permission", "view_inventory"),
                new("permission", "record_stock_movements"),
                new("permission", "view_stock_movements"),
                new("permission", "manage_reservations")
            },
            _ => new List<Claim>()
        };
    }
}