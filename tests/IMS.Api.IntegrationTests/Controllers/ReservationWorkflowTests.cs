using System.Net;
using System.Text.Json;
using FluentAssertions;
using IMS.Api.IntegrationTests.Infrastructure;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Infrastructure.Data.DTOs;
using Microsoft.Extensions.DependencyInjection;
using IMS.Api.Infrastructure.Data;

namespace IMS.Api.IntegrationTests.Controllers;

public class ReservationWorkflowTests : IClassFixture<IntegrationTestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ReservationWorkflowTests(IntegrationTestWebApplicationFactory<Program> factory)
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
    public async Task CompleteReservationWorkflow_ShouldExecuteSuccessfully()
    {
        // Arrange - Get test data and set up inventory
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
            Reason = "Initial stock for reservation test"
        };

        var openingBalanceContent = new StringContent(
            JsonSerializer.Serialize(openingBalanceRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var openingBalanceResponse = await _client.PostAsync("/api/v1/inventory/opening-balance", openingBalanceContent);
        openingBalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Create Reservation
        var reservationRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 100,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            ReferenceNumber = "ORDER-12345",
            Notes = "Customer order reservation"
        };

        var reservationContent = new StringContent(
            JsonSerializer.Serialize(reservationRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var reservationResponse = await _client.PostAsync("/api/v1/reservations", reservationContent);
        reservationResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var reservationResponseContent = await reservationResponse.Content.ReadAsStringAsync();
        var createdReservation = JsonSerializer.Deserialize<ReservationDto>(reservationResponseContent, _jsonOptions);
        
        createdReservation.Should().NotBeNull();
        createdReservation!.Quantity.Should().Be(100);
        createdReservation.Status.Should().Be("Active");

        // Step 3: Verify Inventory Shows Reserved Stock
        var inventoryResponse = await _client.GetAsync($"/api/v1/inventory/{variant.Id}/warehouse/{warehouse.Id}");
        inventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var inventoryContent = await inventoryResponse.Content.ReadAsStringAsync();
        var inventory = JsonSerializer.Deserialize<InventoryItemDto>(inventoryContent, _jsonOptions);
        
        inventory.Should().NotBeNull();
        inventory!.TotalStock.Should().Be(1000);
        inventory.ReservedStock.Should().Be(100);
        inventory.AvailableStock.Should().Be(900); // 1000 - 100

        // Step 4: Modify Reservation
        var modifyRequest = new
        {
            Quantity = 150, // Increase reservation
            ExpiresAtUtc = DateTime.UtcNow.AddDays(10),
            Notes = "Updated customer order"
        };

        var modifyContent = new StringContent(
            JsonSerializer.Serialize(modifyRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var modifyResponse = await _client.PutAsync($"/api/v1/reservations/{createdReservation.Id}", modifyContent);
        modifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Verify Updated Inventory
        var updatedInventoryResponse = await _client.GetAsync($"/api/v1/inventory/{variant.Id}/warehouse/{warehouse.Id}");
        updatedInventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedInventoryContent = await updatedInventoryResponse.Content.ReadAsStringAsync();
        var updatedInventory = JsonSerializer.Deserialize<InventoryItemDto>(updatedInventoryContent, _jsonOptions);
        
        updatedInventory.Should().NotBeNull();
        updatedInventory!.ReservedStock.Should().Be(150);
        updatedInventory.AvailableStock.Should().Be(850); // 1000 - 150

        // Step 6: Cancel Reservation
        var cancelRequest = new
        {
            CancellationReason = "Customer cancelled order"
        };

        var cancelContent = new StringContent(
            JsonSerializer.Serialize(cancelRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var cancelResponse = await _client.PostAsync($"/api/v1/reservations/{createdReservation.Id}/cancel", cancelContent);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Step 7: Verify Inventory After Cancellation
        var finalInventoryResponse = await _client.GetAsync($"/api/v1/inventory/{variant.Id}/warehouse/{warehouse.Id}");
        finalInventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalInventoryContent = await finalInventoryResponse.Content.ReadAsStringAsync();
        var finalInventory = JsonSerializer.Deserialize<InventoryItemDto>(finalInventoryContent, _jsonOptions);
        
        finalInventory.Should().NotBeNull();
        finalInventory!.ReservedStock.Should().Be(0);
        finalInventory.AvailableStock.Should().Be(1000); // Back to full availability
    }

    [Fact]
    public async Task InsufficientStockReservation_ShouldReturnBadRequest()
    {
        // Arrange - Get test data and set up small inventory
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouse = context.Warehouses.First();
        var variant = context.Variants.Skip(1).First(); // Use different variant

        // Step 1: Set Small Opening Balance
        var openingBalanceRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 50,
            Reason = "Small stock for insufficient test"
        };

        var openingBalanceContent = new StringContent(
            JsonSerializer.Serialize(openingBalanceRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var openingBalanceResponse = await _client.PostAsync("/api/v1/inventory/opening-balance", openingBalanceContent);
        openingBalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Attempt Large Reservation
        var reservationRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 100, // More than available
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            ReferenceNumber = "ORDER-FAIL",
            Notes = "Should fail due to insufficient stock"
        };

        var reservationContent = new StringContent(
            JsonSerializer.Serialize(reservationRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var reservationResponse = await _client.PostAsync("/api/v1/reservations", reservationContent);
        reservationResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await reservationResponse.Content.ReadAsStringAsync();
        errorContent.Should().Contain("INSUFFICIENT_STOCK");
    }

    [Fact]
    public async Task GetReservationsByReference_ShouldReturnCorrectReservations()
    {
        // Arrange - Get test data and set up inventory
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
            Reason = "Initial stock for reference test"
        };

        var openingBalanceContent = new StringContent(
            JsonSerializer.Serialize(openingBalanceRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var openingBalanceResponse = await _client.PostAsync("/api/v1/inventory/opening-balance", openingBalanceContent);
        openingBalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Create Multiple Reservations with Same Reference
        var referenceNumber = "ORDER-MULTI-123";
        
        var reservation1Request = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 50,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            ReferenceNumber = referenceNumber,
            Notes = "First item in order"
        };

        var reservation1Content = new StringContent(
            JsonSerializer.Serialize(reservation1Request),
            System.Text.Encoding.UTF8,
            "application/json");

        var reservation1Response = await _client.PostAsync("/api/v1/reservations", reservation1Content);
        reservation1Response.StatusCode.Should().Be(HttpStatusCode.Created);

        var reservation2Request = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 75,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            ReferenceNumber = referenceNumber,
            Notes = "Second item in order"
        };

        var reservation2Content = new StringContent(
            JsonSerializer.Serialize(reservation2Request),
            System.Text.Encoding.UTF8,
            "application/json");

        var reservation2Response = await _client.PostAsync("/api/v1/reservations", reservation2Content);
        reservation2Response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Step 3: Get Reservations by Reference
        var byReferenceResponse = await _client.GetAsync($"/api/v1/reservations/by-reference/{referenceNumber}");
        byReferenceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var byReferenceContent = await byReferenceResponse.Content.ReadAsStringAsync();
        var reservations = JsonSerializer.Deserialize<List<ReservationDto>>(byReferenceContent, _jsonOptions);
        
        reservations.Should().NotBeNull();
        reservations!.Should().HaveCount(2);
        reservations.All(r => r.ReferenceNumber == referenceNumber).Should().BeTrue();
        reservations.Sum(r => r.Quantity).Should().Be(125); // 50 + 75
    }

    [Fact]
    public async Task GetActiveReservations_ShouldReturnOnlyActiveReservations()
    {
        // Arrange - Get test data and set up inventory
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouse = context.Warehouses.First();
        var variant = context.Variants.Skip(1).First(); // Use different variant

        // Step 1: Set Opening Balance
        var openingBalanceRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 1000,
            Reason = "Initial stock for active test"
        };

        var openingBalanceContent = new StringContent(
            JsonSerializer.Serialize(openingBalanceRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var openingBalanceResponse = await _client.PostAsync("/api/v1/inventory/opening-balance", openingBalanceContent);
        openingBalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Create Active Reservation
        var activeReservationRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 100,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            ReferenceNumber = "ACTIVE-ORDER",
            Notes = "Active reservation"
        };

        var activeReservationContent = new StringContent(
            JsonSerializer.Serialize(activeReservationRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var activeReservationResponse = await _client.PostAsync("/api/v1/reservations", activeReservationContent);
        activeReservationResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var activeReservationResponseContent = await activeReservationResponse.Content.ReadAsStringAsync();
        var activeReservation = JsonSerializer.Deserialize<ReservationDto>(activeReservationResponseContent, _jsonOptions);

        // Step 3: Create and Cancel Another Reservation
        var cancelledReservationRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 50,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            ReferenceNumber = "CANCELLED-ORDER",
            Notes = "Will be cancelled"
        };

        var cancelledReservationContent = new StringContent(
            JsonSerializer.Serialize(cancelledReservationRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var cancelledReservationResponse = await _client.PostAsync("/api/v1/reservations", cancelledReservationContent);
        cancelledReservationResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var cancelledReservationResponseContent = await cancelledReservationResponse.Content.ReadAsStringAsync();
        var cancelledReservation = JsonSerializer.Deserialize<ReservationDto>(cancelledReservationResponseContent, _jsonOptions);

        // Cancel the second reservation
        var cancelRequest = new
        {
            CancellationReason = "Customer cancelled"
        };

        var cancelContent = new StringContent(
            JsonSerializer.Serialize(cancelRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var cancelResponse = await _client.PostAsync($"/api/v1/reservations/{cancelledReservation!.Id}/cancel", cancelContent);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Step 4: Get Active Reservations
        var activeReservationsResponse = await _client.GetAsync($"/api/v1/reservations/active?warehouseId={warehouse.Id}");
        activeReservationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var activeReservationsContent = await activeReservationsResponse.Content.ReadAsStringAsync();
        var activeReservations = JsonSerializer.Deserialize<List<ReservationDto>>(activeReservationsContent, _jsonOptions);
        
        activeReservations.Should().NotBeNull();
        activeReservations!.Should().HaveCount(1);
        activeReservations.First().Id.Should().Be(activeReservation!.Id);
        activeReservations.First().Status.Should().Be("Active");
    }
}