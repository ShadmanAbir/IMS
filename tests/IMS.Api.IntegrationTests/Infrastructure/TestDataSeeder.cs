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

        // Create test categories
        var categories = CreateTestCategories(testTenantId);
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        // Create test warehouses
        var warehouses = CreateTestWarehouses(testTenantId);
        context.Warehouses.AddRange(warehouses);
        await context.SaveChangesAsync();

        // Create test products and variants
        var (products, variants) = CreateTestProductsAndVariants(testTenantId, categories);
        context.Products.AddRange(products);
        context.Variants.AddRange(variants);
        await context.SaveChangesAsync();

        // Create test inventory items
        var inventoryItems = CreateTestInventoryItems(variants, warehouses, testTenantId);
        context.InventoryItems.AddRange(inventoryItems);
        await context.SaveChangesAsync();

        // Create test stock movements
        var stockMovements = CreateTestStockMovements(inventoryItems, testUsers.First().Id, testTenantId);
        context.StockMovements.AddRange(stockMovements);
        await context.SaveChangesAsync();

        // Create test reservations
        var reservations = CreateTestReservations(variants, warehouses, testUsers.First().Id, testTenantId);
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
            TenantId = tenantId,
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
            TenantId = tenantId,
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

    private static List<Category> CreateTestCategories(Guid tenantId)
    {
        return new List<Category>
        {
            new Category
            {
                Id = Guid.NewGuid(),
                Name = "Electronics",
                Description = "Electronic products",
                TenantId = tenantId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new Category
            {
                Id = Guid.NewGuid(),
                Name = "Clothing",
                Description = "Clothing items",
                TenantId = tenantId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        };
    }

    private static List<Warehouse> CreateTestWarehouses(Guid tenantId)
    {
        return new List<Warehouse>
        {
            new Warehouse
            {
                Id = Guid.NewGuid(),
                Name = "Main Warehouse",
                Code = "MAIN",
                Address = "123 Main St",
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                Country = "Test Country",
                TenantId = tenantId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new Warehouse
            {
                Id = Guid.NewGuid(),
                Name = "Secondary Warehouse",
                Code = "SEC",
                Address = "456 Second St",
                City = "Test City",
                State = "TS",
                PostalCode = "12346",
                Country = "Test Country",
                TenantId = tenantId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        };
    }

    private static (List<Product>, List<Variant>) CreateTestProductsAndVariants(Guid tenantId, List<Category> categories)
    {
        var products = new List<Product>();
        var variants = new List<Variant>();

        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Laptop",
            Description = "A test laptop product",
            CategoryId = categories.First().Id,
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        products.Add(product1);

        var variant1 = new Variant
        {
            Id = Guid.NewGuid(),
            ProductId = product1.Id,
            SKU = "LAPTOP-001",
            Name = "Test Laptop - 16GB RAM",
            BaseUnit = "each",
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        variants.Add(variant1);

        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test T-Shirt",
            Description = "A test t-shirt product",
            CategoryId = categories.Last().Id,
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        products.Add(product2);

        var variant2 = new Variant
        {
            Id = Guid.NewGuid(),
            ProductId = product2.Id,
            SKU = "TSHIRT-001",
            Name = "Test T-Shirt - Medium",
            BaseUnit = "each",
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        variants.Add(variant2);

        return (products, variants);
    }

    private static List<InventoryItem> CreateTestInventoryItems(List<Variant> variants, List<Warehouse> warehouses, Guid tenantId)
    {
        var inventoryItems = new List<InventoryItem>();

        foreach (var variant in variants)
        {
            foreach (var warehouse in warehouses)
            {
                var inventoryItem = new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    VariantId = variant.Id,
                    WarehouseId = warehouse.Id,
                    TotalStock = 100,
                    ReservedStock = 10,
                    AllowNegativeStock = false,
                    TenantId = tenantId,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                inventoryItems.Add(inventoryItem);
            }
        }

        return inventoryItems;
    }

    private static List<StockMovement> CreateTestStockMovements(List<InventoryItem> inventoryItems, Guid actorId, Guid tenantId)
    {
        var stockMovements = new List<StockMovement>();

        foreach (var inventoryItem in inventoryItems)
        {
            // Opening balance
            var openingBalance = new StockMovement
            {
                Id = Guid.NewGuid(),
                InventoryItemId = inventoryItem.Id,
                MovementType = MovementType.OpeningBalance,
                Quantity = 100,
                RunningBalance = 100,
                Reason = "Initial stock",
                ActorId = actorId,
                TimestampUtc = DateTime.UtcNow.AddDays(-30),
                ReferenceNumber = $"OB-{inventoryItem.Id:N}",
                TenantId = tenantId
            };
            stockMovements.Add(openingBalance);
        }

        return stockMovements;
    }

    private static List<Reservation> CreateTestReservations(List<Variant> variants, List<Warehouse> warehouses, Guid createdBy, Guid tenantId)
    {
        var reservations = new List<Reservation>();

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            VariantId = variants.First().Id,
            WarehouseId = warehouses.First().Id,
            Quantity = 5,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            Status = ReservationStatus.Active,
            ReferenceNumber = "TEST-RES-001",
            CreatedBy = createdBy,
            CreatedAtUtc = DateTime.UtcNow,
            TenantId = tenantId
        };
        reservations.Add(reservation);

        return reservations;
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