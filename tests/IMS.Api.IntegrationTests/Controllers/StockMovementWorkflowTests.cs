using System.Net;
using System.Text.Json;
using FluentAssertions;
using IMS.Api.IntegrationTests.Infrastructure;
using IMS.Api.Application.Common.DTOs;
using Microsoft.Extensions.DependencyInjection;
using IMS.Api.Infrastructure.Data;

namespace IMS.Api.IntegrationTests.Controllers;

public class StockMovementWorkflowTests : IClassFixture<IntegrationTestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public StockMovementWorkflowTests(IntegrationTestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task InitializeAsync()
    {
        // Seed test data
        using var scope = _factory.Services.CreateScope();
        await TestDataSeeder.SeedTestDataAsync(scope.ServiceProvider);
        
        // Authenticate client
        var token = await AuthenticationHelper.GetJwtTokenAsync(_client);
        AuthenticationHelper.SetAuthorizationHeader(_client, token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CompleteStockMovementWorkflow_ShouldExecuteSuccessfully()
    {
        // Arrange - Get test data
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouse = context.Warehouses.First();
        var variant = context.Variants.First();

        // Step 1: Set Opening Balance
        var openingBalanceRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 1000,
            Reason = "Initial stock for integration test"
        };

        var openingBalanceContent = new StringContent(
            JsonSerializer.Serialize(openingBalanceRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var openingBalanceResponse = await _client.PostAsync("/api/v1/inventory/opening-balance", openingBalanceContent);
        openingBalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Record Purchase
        var purchaseRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 500,
            Reason = "Purchase order #12345",
            ReferenceNumber = "PO-12345"
        };

        var purchaseContent = new StringContent(
            JsonSerializer.Serialize(purchaseRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var purchaseResponse = await _client.PostAsync("/api/v1/inventory/purchase", purchaseContent);
        purchaseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Verify Inventory Level
        var inventoryResponse = await _client.GetAsync($"/api/v1/inventory/{variant.Id}/warehouse/{warehouse.Id}");
        inventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var inventoryContent = await inventoryResponse.Content.ReadAsStringAsync();
        var inventory = JsonSerializer.Deserialize<InventoryItemDto>(inventoryContent, _jsonOptions);
        
        inventory.Should().NotBeNull();
        inventory!.TotalStock.Should().Be(1500); // 1000 + 500
        inventory.AvailableStock.Should().Be(1500); // No reservations yet

        // Step 4: Record Sale
        var saleRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 200,
            Reason = "Sale order #67890",
            ReferenceNumber = "SO-67890"
        };

        var saleContent = new StringContent(
            JsonSerializer.Serialize(saleRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var saleResponse = await _client.PostAsync("/api/v1/inventory/sale", saleContent);
        saleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Verify Updated Inventory Level
        var updatedInventoryResponse = await _client.GetAsync($"/api/v1/inventory/{variant.Id}/warehouse/{warehouse.Id}");
        updatedInventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedInventoryContent = await updatedInventoryResponse.Content.ReadAsStringAsync();
        var updatedInventory = JsonSerializer.Deserialize<InventoryItemDto>(updatedInventoryContent, _jsonOptions);
        
        updatedInventory.Should().NotBeNull();
        updatedInventory!.TotalStock.Should().Be(1300); // 1500 - 200
        updatedInventory.AvailableStock.Should().Be(1300);

        // Step 6: Record Stock Adjustment
        var adjustmentRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = -50, // Negative adjustment (write-off)
            Reason = "Damaged goods write-off",
            ReferenceNumber = "ADJ-001"
        };

        var adjustmentContent = new StringContent(
            JsonSerializer.Serialize(adjustmentRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var adjustmentResponse = await _client.PostAsync("/api/v1/inventory/adjustment", adjustmentContent);
        adjustmentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 7: Final Inventory Verification
        var finalInventoryResponse = await _client.GetAsync($"/api/v1/inventory/{variant.Id}/warehouse/{warehouse.Id}");
        finalInventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalInventoryContent = await finalInventoryResponse.Content.ReadAsStringAsync();
        var finalInventory = JsonSerializer.Deserialize<InventoryItemDto>(finalInventoryContent, _jsonOptions);
        
        finalInventory.Should().NotBeNull();
        finalInventory!.TotalStock.Should().Be(1250); // 1300 - 50
        finalInventory.AvailableStock.Should().Be(1250);
    }

    [Fact]
    public async Task StockTransferWorkflow_ShouldExecuteSuccessfully()
    {
        // Arrange - Get test data
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouses = context.Warehouses.Take(2).ToList();
        var sourceWarehouse = warehouses[0];
        var destinationWarehouse = warehouses[1];
        var variant = context.Variants.First();

        // Step 1: Set Opening Balance in Source Warehouse
        var openingBalanceRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = sourceWarehouse.Id,
            Quantity = 500,
            Reason = "Initial stock for transfer test"
        };

        var openingBalanceContent = new StringContent(
            JsonSerializer.Serialize(openingBalanceRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var openingBalanceResponse = await _client.PostAsync("/api/v1/inventory/opening-balance", openingBalanceContent);
        openingBalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Record Transfer
        var transferRequest = new
        {
            VariantId = variant.Id,
            SourceWarehouseId = sourceWarehouse.Id,
            DestinationWarehouseId = destinationWarehouse.Id,
            Quantity = 100,
            Reason = "Warehouse rebalancing",
            ReferenceNumber = "TRF-001"
        };

        var transferContent = new StringContent(
            JsonSerializer.Serialize(transferRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var transferResponse = await _client.PostAsync("/api/v1/inventory/transfer", transferContent);
        transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Verify Source Warehouse Inventory
        var sourceInventoryResponse = await _client.GetAsync($"/api/v1/inventory/{variant.Id}/warehouse/{sourceWarehouse.Id}");
        sourceInventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sourceInventoryContent = await sourceInventoryResponse.Content.ReadAsStringAsync();
        var sourceInventory = JsonSerializer.Deserialize<InventoryItemDto>(sourceInventoryContent, _jsonOptions);
        
        sourceInventory.Should().NotBeNull();
        sourceInventory!.TotalStock.Should().Be(400); // 500 - 100

        // Step 4: Verify Destination Warehouse Inventory
        var destInventoryResponse = await _client.GetAsync($"/api/v1/inventory/{variant.Id}/warehouse/{destinationWarehouse.Id}");
        destInventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var destInventoryContent = await destInventoryResponse.Content.ReadAsStringAsync();
        var destInventory = JsonSerializer.Deserialize<InventoryItemDto>(destInventoryContent, _jsonOptions);
        
        destInventory.Should().NotBeNull();
        destInventory!.TotalStock.Should().Be(100); // Transferred amount
    }

    [Fact]
    public async Task OutOfStockScenario_ShouldReturnAppropriateError()
    {
        // Arrange - Get test data
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouse = context.Warehouses.First();
        var variant = context.Variants.Skip(1).First(); // Use different variant

        // Step 1: Set Small Opening Balance
        var openingBalanceRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 10,
            Reason = "Small initial stock for out-of-stock test"
        };

        var openingBalanceContent = new StringContent(
            JsonSerializer.Serialize(openingBalanceRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var openingBalanceResponse = await _client.PostAsync("/api/v1/inventory/opening-balance", openingBalanceContent);
        openingBalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Attempt Large Sale (Should Fail)
        var saleRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 50, // More than available
            Reason = "Large sale order",
            ReferenceNumber = "SO-FAIL"
        };

        var saleContent = new StringContent(
            JsonSerializer.Serialize(saleRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var saleResponse = await _client.PostAsync("/api/v1/inventory/sale", saleContent);
        saleResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await saleResponse.Content.ReadAsStringAsync();
        errorContent.Should().Contain("OUT_OF_STOCK");
    }

    [Fact]
    public async Task DuplicateOpeningBalance_ShouldReturnConflict()
    {
        // Arrange - Get test data
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouse = context.Warehouses.First();
        var variant = context.Variants.Skip(2).First(); // Use different variant

        // Step 1: Set Opening Balance
        var openingBalanceRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 100,
            Reason = "Initial stock"
        };

        var openingBalanceContent = new StringContent(
            JsonSerializer.Serialize(openingBalanceRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var firstResponse = await _client.PostAsync("/api/v1/inventory/opening-balance", openingBalanceContent);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Attempt Duplicate Opening Balance
        var secondResponse = await _client.PostAsync("/api/v1/inventory/opening-balance", openingBalanceContent);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var errorContent = await secondResponse.Content.ReadAsStringAsync();
        errorContent.Should().Contain("OPENING_BALANCE_EXISTS");
    }
}