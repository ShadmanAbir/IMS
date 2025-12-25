using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using IMS.Api.Infrastructure.Data;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Domain.Enums;
using Bogus;

namespace IMS.Api.IntegrationTests.Infrastructure;

public static class TestDataSeeder
{
    public static async Task SeedTestDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Clear existing data
        context.StockMovements.RemoveRange(context.StockMovements);
        context.InventoryItems.RemoveRange(context.InventoryItems);
        context.Reservations.RemoveRange(context.Reservations);
        context.Variants.RemoveRange(context.Variants);
        context.Products.RemoveRange(context.Products);
        context.Categories.RemoveRange(context.Categories);
        context.Warehouses.RemoveRange(context.Warehouses);
        await context.SaveChangesAsync();

        // Create test tenant
        var testTenantId = Guid.NewGuid();

        // Create roles
        await CreateRolesAsync(roleManager);

        // Create test users
        var testUsers = await CreateTestUsersAsync(userManager, testTenantId);
        var systemUser = testUsers.First();

        // Create test categories via domain factories
        var categories = new List<Category>
        {
            Category.CreateRootCategory("Electronics", "ELEC", "Electronic products", 0, true, TenantId.Create(testTenantId), UserId.Create(systemUser.Id)),
            Category.CreateRootCategory("Clothing", "CLOTH", "Clothing items", 1, true, TenantId.Create(testTenantId), UserId.Create(systemUser.Id))
        };
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        // Create test warehouses via factories
        var warehouses = new List<Warehouse>
        {
            Warehouse.Create("Main Warehouse", "MAIN", string.Empty, "123 Main St", "Test City", "TS", "Test Country", "12345", null, null, true, null, null, TenantId.Create(testTenantId), UserId.Create(systemUser.Id)),
            Warehouse.Create("Secondary Warehouse", "SEC", string.Empty, "456 Second St", "Test City", "TS", "Test Country", "12346", null, null, true, null, null, TenantId.Create(testTenantId), UserId.Create(systemUser.Id))
        };
        context.Warehouses.AddRange(warehouses);
        await context.SaveChangesAsync();

        // Create test products and variants using factories
        var products = new List<Product>();
        var variants = new List<Variant>();

        var product1 = Product.Create("Test Laptop", "A test laptop product", TenantId.Create(testTenantId), categories.First().Id);
        products.Add(product1);
        var variant1 = Variant.Create(SKU.Create("LAPTOP-001"), "Test Laptop - 16GB RAM", UnitOfMeasure.Piece, product1.Id);
        variants.Add(variant1);

        var product2 = Product.Create("Test T-Shirt", "A test t-shirt product", TenantId.Create(testTenantId), categories.Last().Id);
        products.Add(product2);
        var variant2 = Variant.Create(SKU.Create("TSHIRT-001"), "Test T-Shirt - Medium", UnitOfMeasure.Piece, product2.Id);
        variants.Add(variant2);

        context.Products.AddRange(products);
        context.Variants.AddRange(variants);
        await context.SaveChangesAsync();

        // Create test inventory items and set opening balances
        var inventoryItems = new List<InventoryItem>();

        foreach (var variant in variants)
        {
            foreach (var warehouse in warehouses)
            {
                var inventoryItem = InventoryItem.Create(variant.Id, warehouse.Id);
                // Set opening balance to 100 for seeded data
                inventoryItem.SetOpeningBalance(100, "Initial stock", UserId.Create(systemUser.Id), $"OB-{Guid.NewGuid():N}");
                inventoryItems.Add(inventoryItem);
            }
        }

        context.InventoryItems.AddRange(inventoryItems);
        await context.SaveChangesAsync();

        // Create test reservations via domain factory
        var reservations = new List<Reservation>
        {
            Reservation.Create(variants.First().Id, warehouses.First().Id, 5, DateTime.UtcNow.AddDays(7), "TEST-RES-001", "Test reservation", TenantId.Create(testTenantId), UserId.Create(systemUser.Id))
        };

        context.Reservations.AddRange(reservations);
        await context.SaveChangesAsync();
    }

    private static async Task CreateRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roles = new[]
        {
            "SystemAdmin",
            "TenantAdmin",
            "WarehouseManager",
            "InventoryAnalyst",
            "WarehouseOperator"
        };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }
        }
    }

    private static async Task<List<ApplicationUser>> CreateTestUsersAsync(UserManager<ApplicationUser> userManager, Guid tenantId)
    {
        var users = new List<ApplicationUser>();

        var testUser = new ApplicationUser
        {
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            TenantId = TenantId.Create(tenantId),
            FirstName = "Test",
            LastName = "User"
        };

        var result = await userManager.CreateAsync(testUser, "TestPass123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(testUser, "SystemAdmin");
            users.Add(testUser);
        }

        var warehouseOperator = new ApplicationUser
        {
            UserName = "operator@example.com",
            Email = "operator@example.com",
            EmailConfirmed = true,
            TenantId = TenantId.Create(tenantId),
            FirstName = "Warehouse",
            LastName = "Operator"
        };

        result = await userManager.CreateAsync(warehouseOperator, "TestPass123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(warehouseOperator, "WarehouseOperator");
            users.Add(warehouseOperator);
        }

        return users;
    }

    public static class TestData
    {
        public static Guid TestTenantId { get; set; }
        public static List<Guid> TestUserIds { get; set; } = new();
        public static List<Guid> TestWarehouseIds { get; set; } = new();
        public static List<Guid> TestVariantIds { get; set; } = new();
        public static List<Guid> TestProductIds { get; set; } = new();
        public static List<Guid> TestCategoryIds { get; set; } = new();
    }
}