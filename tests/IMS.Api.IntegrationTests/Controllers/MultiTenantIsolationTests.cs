using System.Net;
using System.Text.Json;
using FluentAssertions;
using IMS.Api.IntegrationTests.Infrastructure;
using IMS.Api.Application.Common.DTOs;
using Microsoft.Extensions.DependencyInjection;
using IMS.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using IMS.Api.Domain.Entities;

namespace IMS.Api.IntegrationTests.Controllers;

public class MultiTenantIsolationTests : IClassFixture<IntegrationTestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory<Program> _factory;
    private readonly JsonSerializerOptions _jsonOptions;
    private HttpClient _tenant1Client = null!;
    private HttpClient _tenant2Client = null!;
    private Guid _tenant1Id;
    private Guid _tenant2Id;
    private Guid _tenant1WarehouseId;
    private Guid _tenant2WarehouseId;
    private Guid _tenant1VariantId;
    private Guid _tenant2VariantId;

    public MultiTenantIsolationTests(IntegrationTestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task InitializeAsync()
    {
        // Create separate tenant data
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Create roles if they don't exist
        var roles = new[] { "SystemAdmin", "TenantAdmin", "WarehouseManager", "InventoryAnalyst", "WarehouseOperator" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }
        }

        // Create Tenant 1 data
        _tenant1Id = Guid.NewGuid();
        await CreateTenantDataAsync(context, userManager, _tenant1Id, "tenant1@example.com", "Tenant1");

        // Create Tenant 2 data
        _tenant2Id = Guid.NewGuid();
        await CreateTenantDataAsync(context, userManager, _tenant2Id, "tenant2@example.com", "Tenant2");

        // Create authenticated clients for each tenant
        _tenant1Client = _factory.CreateClient();
        var tenant1Token = await AuthenticationHelper.GetJwtTokenAsync(_tenant1Client, "tenant1@example.com");
        AuthenticationHelper.SetAuthorizationHeader(_tenant1Client, tenant1Token);

        _tenant2Client = _factory.CreateClient();
        var tenant2Token = await AuthenticationHelper.GetJwtTokenAsync(_tenant2Client, "tenant2@example.com");
        AuthenticationHelper.SetAuthorizationHeader(_tenant2Client, tenant2Token);
    }

    public Task DisposeAsync()
    {
        _tenant1Client?.Dispose();
        _tenant2Client?.Dispose();
        return Task.CompletedTask;
    }

    private async Task CreateTenantDataAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, Guid tenantId, string email, string tenantName)
    {
        // Create user for tenant
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            TenantId = tenantId,
            FirstName = tenantName,
            LastName = "User"
        };

        var result = await userManager.CreateAsync(user, "TestPass123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "SystemAdmin");
        }

        // Create category for tenant
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = $"{tenantName} Electronics",
            Description = $"Electronics for {tenantName}",
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Categories.Add(category);

        // Create warehouse for tenant
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = $"{tenantName} Warehouse",
            Code = $"{tenantName.ToUpper()}WH",
            Address = $"123 {tenantName} St",
            City = $"{tenantName} City",
            State = "TS",
            PostalCode = "12345",
            Country = "Test Country",
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Warehouses.Add(warehouse);

        // Create product for tenant
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = $"{tenantName} Laptop",
            Description = $"Laptop for {tenantName}",
            CategoryId = category.Id,
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Products.Add(product);

        // Create variant for tenant
        var variant = new Variant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            SKU = $"{tenantName.ToUpper()}-LAPTOP-001",
            Name = $"{tenantName} Laptop - 16GB",
            BaseUnit = "each",
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Variants.Add(variant);

        await context.SaveChangesAsync();

        // Store IDs for tests
        if (tenantId == _tenant1Id)
        {
            _tenant1WarehouseId = warehouse.Id;
            _tenant1VariantId = variant.Id;
        }
        else
        {
            _tenant2WarehouseId = warehouse.Id;
            _tenant2VariantId = variant.Id;
        }
    }

    [Fact]
    public async Task TenantDataIsolation_ProductsEndpoint_ShouldOnlyReturnTenantData()
    {
        // Act - Get products for each tenant
        var tenant1ProductsResponse = await _tenant1Client.GetAsync("/api/v1/products");
        var tenant2ProductsResponse = await _tenant2Client.GetAsync("/api/v1/products");

        // Assert
        tenant1ProductsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        tenant2ProductsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tenant1ProductsContent = await tenant1ProductsResponse.Content.ReadAsStringAsync();
        var tenant2ProductsContent = await tenant2ProductsResponse.Content.ReadAsStringAsync();

        var tenant1Products = JsonSerializer.Deserialize<PagedResult<ProductDto>>(tenant1ProductsContent, _jsonOptions);
        var tenant2Products = JsonSerializer.Deserialize<PagedResult<ProductDto>>(tenant2ProductsContent, _jsonOptions);

        // Each tenant should only see their own products
        tenant1Products.Should().NotBeNull();
        tenant1Products!.Items.Should().HaveCount(1);
        tenant1Products.Items.First().Name.Should().Contain("Tenant1");

        tenant2Products.Should().NotBeNull();
        tenant2Products!.Items.Should().HaveCount(1);
        tenant2Products.Items.First().Name.Should().Contain("Tenant2");

        // Verify no cross-tenant data leakage
        tenant1Products.Items.Should().NotContain(p => p.Name.Contains("Tenant2"));
        tenant2Products.Items.Should().NotContain(p => p.Name.Contains("Tenant1"));
    }

    [Fact]
    public async Task TenantDataIsolation_InventoryEndpoint_ShouldOnlyReturnTenantData()
    {
        // Arrange - Set up inventory for both tenants
        var tenant1OpeningBalance = new
        {
            VariantId = _tenant1VariantId,
            WarehouseId = _tenant1WarehouseId,
            Quantity = 100,
            Reason = "Tenant 1 initial stock"
        };

        var tenant2OpeningBalance = new
        {
            VariantId = _tenant2VariantId,
            WarehouseId = _tenant2WarehouseId,
            Quantity = 200,
            Reason = "Tenant 2 initial stock"
        };

        var tenant1Content = new StringContent(
            JsonSerializer.Serialize(tenant1OpeningBalance),
            System.Text.Encoding.UTF8,
            "application/json");

        var tenant2Content = new StringContent(
            JsonSerializer.Serialize(tenant2OpeningBalance),
            System.Text.Encoding.UTF8,
            "application/json");

        // Set opening balances
        var tenant1BalanceResponse = await _tenant1Client.PostAsync("/api/v1/inventory/opening-balance", tenant1Content);
        var tenant2BalanceResponse = await _tenant2Client.PostAsync("/api/v1/inventory/opening-balance", tenant2Content);

        tenant1BalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        tenant2BalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Get inventory for each tenant
        var tenant1InventoryResponse = await _tenant1Client.GetAsync("/api/v1/inventory");
        var tenant2InventoryResponse = await _tenant2Client.GetAsync("/api/v1/inventory");

        // Assert
        tenant1InventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        tenant2InventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tenant1InventoryContent = await tenant1InventoryResponse.Content.ReadAsStringAsync();
        var tenant2InventoryContent = await tenant2InventoryResponse.Content.ReadAsStringAsync();

        var tenant1Inventory = JsonSerializer.Deserialize<List<InventoryItemDto>>(tenant1InventoryContent, _jsonOptions);
        var tenant2Inventory = JsonSerializer.Deserialize<List<InventoryItemDto>>(tenant2InventoryContent, _jsonOptions);

        // Each tenant should only see their own inventory
        tenant1Inventory.Should().NotBeNull();
        tenant1Inventory!.Should().HaveCount(1);
        tenant1Inventory.First().TotalStock.Should().Be(100);

        tenant2Inventory.Should().NotBeNull();
        tenant2Inventory!.Should().HaveCount(1);
        tenant2Inventory.First().TotalStock.Should().Be(200);
    }

    [Fact]
    public async Task TenantDataIsolation_CrossTenantAccess_ShouldReturnNotFound()
    {
        // Arrange - Set up inventory for tenant 1
        var openingBalance = new
        {
            VariantId = _tenant1VariantId,
            WarehouseId = _tenant1WarehouseId,
            Quantity = 100,
            Reason = "Initial stock"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(openingBalance),
            System.Text.Encoding.UTF8,
            "application/json");

        var balanceResponse = await _tenant1Client.PostAsync("/api/v1/inventory/opening-balance", content);
        balanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Tenant 2 tries to access Tenant 1's inventory
        var crossTenantResponse = await _tenant2Client.GetAsync($"/api/v1/inventory/{_tenant1VariantId}/warehouse/{_tenant1WarehouseId}");

        // Assert - Should return NotFound (not Forbidden to avoid information leakage)
        crossTenantResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantDataIsolation_ReservationsEndpoint_ShouldOnlyReturnTenantData()
    {
        // Arrange - Set up inventory and reservations for both tenants
        var tenant1OpeningBalance = new
        {
            VariantId = _tenant1VariantId,
            WarehouseId = _tenant1WarehouseId,
            Quantity = 100,
            Reason = "Tenant 1 initial stock"
        };

        var tenant2OpeningBalance = new
        {
            VariantId = _tenant2VariantId,
            WarehouseId = _tenant2WarehouseId,
            Quantity = 200,
            Reason = "Tenant 2 initial stock"
        };

        // Set opening balances
        var tenant1BalanceContent = new StringContent(
            JsonSerializer.Serialize(tenant1OpeningBalance),
            System.Text.Encoding.UTF8,
            "application/json");

        var tenant2BalanceContent = new StringContent(
            JsonSerializer.Serialize(tenant2OpeningBalance),
            System.Text.Encoding.UTF8,
            "application/json");

        await _tenant1Client.PostAsync("/api/v1/inventory/opening-balance", tenant1BalanceContent);
        await _tenant2Client.PostAsync("/api/v1/inventory/opening-balance", tenant2BalanceContent);

        // Create reservations
        var tenant1Reservation = new
        {
            VariantId = _tenant1VariantId,
            WarehouseId = _tenant1WarehouseId,
            Quantity = 10,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            ReferenceNumber = "TENANT1-ORDER",
            Notes = "Tenant 1 reservation"
        };

        var tenant2Reservation = new
        {
            VariantId = _tenant2VariantId,
            WarehouseId = _tenant2WarehouseId,
            Quantity = 20,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            ReferenceNumber = "TENANT2-ORDER",
            Notes = "Tenant 2 reservation"
        };

        var tenant1ReservationContent = new StringContent(
            JsonSerializer.Serialize(tenant1Reservation),
            System.Text.Encoding.UTF8,
            "application/json");

        var tenant2ReservationContent = new StringContent(
            JsonSerializer.Serialize(tenant2Reservation),
            System.Text.Encoding.UTF8,
            "application/json");

        await _tenant1Client.PostAsync("/api/v1/reservations", tenant1ReservationContent);
        await _tenant2Client.PostAsync("/api/v1/reservations", tenant2ReservationContent);

        // Act - Get reservations for each tenant
        var tenant1ReservationsResponse = await _tenant1Client.GetAsync("/api/v1/reservations");
        var tenant2ReservationsResponse = await _tenant2Client.GetAsync("/api/v1/reservations");

        // Assert
        tenant1ReservationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        tenant2ReservationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tenant1ReservationsContent = await tenant1ReservationsResponse.Content.ReadAsStringAsync();
        var tenant2ReservationsContent = await tenant2ReservationsResponse.Content.ReadAsStringAsync();

        var tenant1Reservations = JsonSerializer.Deserialize<PagedResult<ReservationDto>>(tenant1ReservationsContent, _jsonOptions);
        var tenant2Reservations = JsonSerializer.Deserialize<PagedResult<ReservationDto>>(tenant2ReservationsContent, _jsonOptions);

        // Each tenant should only see their own reservations
        tenant1Reservations.Should().NotBeNull();
        tenant1Reservations!.Items.Should().HaveCount(1);
        tenant1Reservations.Items.First().ReferenceNumber.Should().Be("TENANT1-ORDER");
        tenant1Reservations.Items.First().Quantity.Should().Be(10);

        tenant2Reservations.Should().NotBeNull();
        tenant2Reservations!.Items.Should().HaveCount(1);
        tenant2Reservations.Items.First().ReferenceNumber.Should().Be("TENANT2-ORDER");
        tenant2Reservations.Items.First().Quantity.Should().Be(20);

        // Verify no cross-tenant data leakage
        tenant1Reservations.Items.Should().NotContain(r => r.ReferenceNumber.Contains("TENANT2"));
        tenant2Reservations.Items.Should().NotContain(r => r.ReferenceNumber.Contains("TENANT1"));
    }

    [Fact]
    public async Task TenantDataIsolation_StockMovements_ShouldOnlyReturnTenantData()
    {
        // Arrange - Set up inventory and stock movements for both tenants
        var tenant1OpeningBalance = new
        {
            VariantId = _tenant1VariantId,
            WarehouseId = _tenant1WarehouseId,
            Quantity = 100,
            Reason = "Tenant 1 initial stock"
        };

        var tenant2OpeningBalance = new
        {
            VariantId = _tenant2VariantId,
            WarehouseId = _tenant2WarehouseId,
            Quantity = 200,
            Reason = "Tenant 2 initial stock"
        };

        // Set opening balances
        var tenant1BalanceContent = new StringContent(
            JsonSerializer.Serialize(tenant1OpeningBalance),
            System.Text.Encoding.UTF8,
            "application/json");

        var tenant2BalanceContent = new StringContent(
            JsonSerializer.Serialize(tenant2OpeningBalance),
            System.Text.Encoding.UTF8,
            "application/json");

        await _tenant1Client.PostAsync("/api/v1/inventory/opening-balance", tenant1BalanceContent);
        await _tenant2Client.PostAsync("/api/v1/inventory/opening-balance", tenant2BalanceContent);

        // Record purchases
        var tenant1Purchase = new
        {
            VariantId = _tenant1VariantId,
            WarehouseId = _tenant1WarehouseId,
            Quantity = 50,
            Reason = "Tenant 1 purchase",
            ReferenceNumber = "TENANT1-PO-001"
        };

        var tenant2Purchase = new
        {
            VariantId = _tenant2VariantId,
            WarehouseId = _tenant2WarehouseId,
            Quantity = 75,
            Reason = "Tenant 2 purchase",
            ReferenceNumber = "TENANT2-PO-001"
        };

        var tenant1PurchaseContent = new StringContent(
            JsonSerializer.Serialize(tenant1Purchase),
            System.Text.Encoding.UTF8,
            "application/json");

        var tenant2PurchaseContent = new StringContent(
            JsonSerializer.Serialize(tenant2Purchase),
            System.Text.Encoding.UTF8,
            "application/json");

        await _tenant1Client.PostAsync("/api/v1/inventory/purchase", tenant1PurchaseContent);
        await _tenant2Client.PostAsync("/api/v1/inventory/purchase", tenant2PurchaseContent);

        // Act - Get stock movement history for each tenant (this would require a stock movements endpoint)
        // For now, verify through inventory levels that movements are isolated
        var tenant1InventoryResponse = await _tenant1Client.GetAsync($"/api/v1/inventory/{_tenant1VariantId}/warehouse/{_tenant1WarehouseId}");
        var tenant2InventoryResponse = await _tenant2Client.GetAsync($"/api/v1/inventory/{_tenant2VariantId}/warehouse/{_tenant2WarehouseId}");

        // Assert
        tenant1InventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        tenant2InventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tenant1InventoryContent = await tenant1InventoryResponse.Content.ReadAsStringAsync();
        var tenant2InventoryContent = await tenant2InventoryResponse.Content.ReadAsStringAsync();

        var tenant1Inventory = JsonSerializer.Deserialize<InventoryItemDto>(tenant1InventoryContent, _jsonOptions);
        var tenant2Inventory = JsonSerializer.Deserialize<InventoryItemDto>(tenant2InventoryContent, _jsonOptions);

        // Verify each tenant's stock reflects only their movements
        tenant1Inventory.Should().NotBeNull();
        tenant1Inventory!.TotalStock.Should().Be(150); // 100 + 50

        tenant2Inventory.Should().NotBeNull();
        tenant2Inventory!.TotalStock.Should().Be(275); // 200 + 75
    }
}