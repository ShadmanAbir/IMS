using System.Net;
using System.Text.Json;
using FluentAssertions;
using IMS.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using IMS.Api.Infrastructure.Data;
using IMS.Api.Application.Common.DTOs;

namespace IMS.Api.IntegrationTests.SignalR;

public class DashboardHubTests : IClassFixture<IntegrationTestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private HubConnection _hubConnection = null!;

    public DashboardHubTests(IntegrationTestWebApplicationFactory<Program> factory)
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

        // Create SignalR connection
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_client.BaseAddress}dashboardHub", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();

        await _hubConnection.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
        _client?.Dispose();
    }

    [Fact]
    public async Task DashboardHub_StockLevelChange_ShouldReceiveRealTimeNotification()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouse = context.Warehouses.First();
        var variant = context.Variants.First();

        var receivedNotifications = new List<object>();
        
        // Subscribe to stock level change notifications
        _hubConnection.On<object>("StockLevelChanged", (notification) =>
        {
            receivedNotifications.Add(notification);
        });

        // Join warehouse group
        await _hubConnection.InvokeAsync("JoinWarehouseGroup", warehouse.Id.ToString());

        // Act - Set opening balance to trigger notification
        var openingBalanceRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 1000,
            Reason = "Initial stock for SignalR test"
        };

        var openingBalanceContent = new StringContent(
            JsonSerializer.Serialize(openingBalanceRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/v1/inventory/opening-balance", openingBalanceContent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for SignalR notification
        await Task.Delay(2000); // Give time for background services to process

        // Assert
        receivedNotifications.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DashboardHub_LowStockAlert_ShouldReceiveRealTimeAlert()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouse = context.Warehouses.First();
        var variant = context.Variants.Skip(1).First(); // Use different variant

        var receivedAlerts = new List<object>();
        
        // Subscribe to low stock alerts
        _hubConnection.On<object>("LowStockAlert", (alert) =>
        {
            receivedAlerts.Add(alert);
        });

        // Join alerts group
        await _hubConnection.InvokeAsync("SubscribeToAlerts", "low-stock");

        // Act - Create low stock scenario
        // Step 1: Set small opening balance
        var openingBalanceRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 15, // Just above typical low stock threshold
            Reason = "Small stock for low stock test"
        };

        var openingBalanceContent = new StringContent(
            JsonSerializer.Serialize(openingBalanceRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var openingBalanceResponse = await _client.PostAsync("/api/v1/inventory/opening-balance", openingBalanceContent);
        openingBalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Make a sale to trigger low stock
        var saleRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 10, // This should bring stock below threshold
            Reason = "Sale to trigger low stock",
            ReferenceNumber = "SO-LOWSTOCK"
        };

        var saleContent = new StringContent(
            JsonSerializer.Serialize(saleRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var saleResponse = await _client.PostAsync("/api/v1/inventory/sale", saleContent);
        saleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for alert detection background service to process
        await Task.Delay(3000);

        // Assert
        // Note: This test depends on the alert detection background service being configured
        // In a real implementation, you might need to trigger the alert detection manually
        // or configure the background service to run more frequently in tests
        receivedAlerts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DashboardHub_ReservationExpiry_ShouldReceiveRealTimeAlert()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouse = context.Warehouses.First();
        var variant = context.Variants.Skip(2).First(); // Use different variant

        var receivedAlerts = new List<object>();
        
        // Subscribe to reservation expiry alerts
        _hubConnection.On<object>("ReservationExpiryAlert", (alert) =>
        {
            receivedAlerts.Add(alert);
        });

        // Join alerts group
        await _hubConnection.InvokeAsync("SubscribeToAlerts", "reservation-expiry");

        // Act - Create expiring reservation scenario
        // Step 1: Set opening balance
        var openingBalanceRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 100,
            Reason = "Stock for reservation expiry test"
        };

        var openingBalanceContent = new StringContent(
            JsonSerializer.Serialize(openingBalanceRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var openingBalanceResponse = await _client.PostAsync("/api/v1/inventory/opening-balance", openingBalanceContent);
        openingBalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Create reservation that expires soon
        var reservationRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 10,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(30), // Expires in 30 seconds
            ReferenceNumber = "EXPIRY-TEST",
            Notes = "Reservation for expiry test"
        };

        var reservationContent = new StringContent(
            JsonSerializer.Serialize(reservationRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var reservationResponse = await _client.PostAsync("/api/v1/reservations", reservationContent);
        reservationResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Wait for reservation to expire and alert to be generated
        await Task.Delay(35000); // Wait 35 seconds

        // Assert
        // Note: This test depends on the reservation expiry background service
        receivedAlerts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DashboardHub_GroupSubscription_ShouldOnlyReceiveRelevantNotifications()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouses = context.Warehouses.Take(2).ToList();
        var warehouse1 = warehouses[0];
        var warehouse2 = warehouses[1];
        var variant = context.Variants.First();

        var warehouse1Notifications = new List<object>();
        var warehouse2Notifications = new List<object>();
        
        // Create second hub connection
        var token = await AuthenticationHelper.GetJwtTokenAsync(_client);
        var hubConnection2 = new HubConnectionBuilder()
            .WithUrl($"{_client.BaseAddress}dashboardHub", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();

        await hubConnection2.StartAsync();

        try
        {
            // Subscribe first connection to warehouse 1
            _hubConnection.On<object>("StockLevelChanged", (notification) =>
            {
                warehouse1Notifications.Add(notification);
            });
            await _hubConnection.InvokeAsync("JoinWarehouseGroup", warehouse1.Id.ToString());

            // Subscribe second connection to warehouse 2
            hubConnection2.On<object>("StockLevelChanged", (notification) =>
            {
                warehouse2Notifications.Add(notification);
            });
            await hubConnection2.InvokeAsync("JoinWarehouseGroup", warehouse2.Id.ToString());

            // Act - Trigger stock change in warehouse 1 only
            var openingBalanceRequest = new
            {
                VariantId = variant.Id,
                WarehouseId = warehouse1.Id,
                Quantity = 500,
                Reason = "Stock for group subscription test"
            };

            var openingBalanceContent = new StringContent(
                JsonSerializer.Serialize(openingBalanceRequest),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync("/api/v1/inventory/opening-balance", openingBalanceContent);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Wait for notifications
            await Task.Delay(2000);

            // Assert
            warehouse1Notifications.Should().NotBeEmpty();
            warehouse2Notifications.Should().BeEmpty(); // Should not receive notifications for other warehouses
        }
        finally
        {
            await hubConnection2.DisposeAsync();
        }
    }

    [Fact]
    public async Task DashboardHub_ConnectionManagement_ShouldHandleConnectionLifecycle()
    {
        // Arrange
        var connectionStateChanges = new List<HubConnectionState>();
        
        _hubConnection.Closed += (error) =>
        {
            connectionStateChanges.Add(HubConnectionState.Disconnected);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += (connectionId) =>
        {
            connectionStateChanges.Add(HubConnectionState.Connected);
            return Task.CompletedTask;
        };

        // Act - Test connection state
        var initialState = _hubConnection.State;
        initialState.Should().Be(HubConnectionState.Connected);

        // Test that we can invoke methods
        await _hubConnection.InvokeAsync("JoinWarehouseGroup", Guid.NewGuid().ToString());

        // The connection should remain stable
        await Task.Delay(1000);
        _hubConnection.State.Should().Be(HubConnectionState.Connected);

        // Assert
        _hubConnection.State.Should().Be(HubConnectionState.Connected);
    }
}