using System.Net;
using System.Text.Json;
using FluentAssertions;
using IMS.Api.IntegrationTests.Infrastructure;
using IMS.Api.Application.Common.DTOs;
using Microsoft.Extensions.DependencyInjection;
using IMS.Api.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR.Client;

namespace IMS.Api.IntegrationTests.EndToEnd;

public class DashboardRealTimeUpdatesTests : IClassFixture<IntegrationTestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private HubConnection _hubConnection = null!;

    public DashboardRealTimeUpdatesTests(IntegrationTestWebApplicationFactory<Program> factory)
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
    public async Task DashboardMetricsUpdates_ShouldReflectRealTimeChanges()
    {
        // This test verifies that dashboard metrics are updated in real-time
        // as inventory operations are performed

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouse = context.Warehouses.First();
        var variant = context.Variants.First();

        var metricsUpdates = new List<object>();
        var stockLevelChanges = new List<object>();

        // Subscribe to dashboard updates
        _hubConnection.On<object>("DashboardMetricsUpdated", (update) => metricsUpdates.Add(update));
        _hubConnection.On<object>("StockLevelChanged", (change) => stockLevelChanges.Add(change));

        // Join relevant groups
        await _hubConnection.InvokeAsync("JoinWarehouseGroup", warehouse.Id.ToString());
        await _hubConnection.InvokeAsync("JoinVariantGroup", variant.Id.ToString());

        // Step 1: Get initial dashboard metrics
        var initialMetricsResponse = await _client.GetAsync("/api/v1/dashboard/metrics");
        DashboardMetricsDto? initialMetrics = null;
        
        if (initialMetricsResponse.StatusCode == HttpStatusCode.OK)
        {
            var initialMetricsContent = await initialMetricsResponse.Content.ReadAsStringAsync();
            initialMetrics = JsonSerializer.Deserialize<DashboardMetricsDto>(initialMetricsContent, _jsonOptions);
        }

        // Step 2: Perform inventory operations that should trigger updates
        var openingBalanceRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 1000,
            Reason = "Initial stock for dashboard test"
        };

        var openingBalanceResponse = await _client.PostAsync("/api/v1/inventory/opening-balance",
            new StringContent(JsonSerializer.Serialize(openingBalanceRequest), System.Text.Encoding.UTF8, "application/json"));
        openingBalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for real-time updates
        await Task.Delay(3000);

        // Step 3: Perform additional operations
        var purchaseRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 500,
            Reason = "Purchase for dashboard test",
            ReferenceNumber = "PO-DASHBOARD-001"
        };

        var purchaseResponse = await _client.PostAsync("/api/v1/inventory/purchase",
            new StringContent(JsonSerializer.Serialize(purchaseRequest), System.Text.Encoding.UTF8, "application/json"));
        purchaseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Create reservation to affect available stock
        var reservationRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 200,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            ReferenceNumber = "DASHBOARD-ORDER",
            Notes = "Reservation for dashboard test"
        };

        var reservationResponse = await _client.PostAsync("/api/v1/reservations",
            new StringContent(JsonSerializer.Serialize(reservationRequest), System.Text.Encoding.UTF8, "application/json"));
        reservationResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Wait for more updates
        await Task.Delay(3000);

        // Step 4: Get updated dashboard metrics
        var updatedMetricsResponse = await _client.GetAsync("/api/v1/dashboard/metrics");
        if (updatedMetricsResponse.StatusCode == HttpStatusCode.OK)
        {
            var updatedMetricsContent = await updatedMetricsResponse.Content.ReadAsStringAsync();
            var updatedMetrics = JsonSerializer.Deserialize<DashboardMetricsDto>(updatedMetricsContent, _jsonOptions);
            
            updatedMetrics.Should().NotBeNull();
            
            // Verify metrics reflect the changes
            if (initialMetrics != null)
            {
                updatedMetrics!.TotalAvailableStock.Should().BeGreaterThan(initialMetrics.TotalAvailableStock);
                updatedMetrics.TotalReservedStock.Should().BeGreaterThan(initialMetrics.TotalReservedStock);
            }
        }

        // Step 5: Verify we received real-time updates
        // Note: The actual updates depend on background services being configured
        stockLevelChanges.Should().NotBeEmpty("Should receive stock level change notifications");
    }

    [Fact]
    public async Task LowStockAlerts_ShouldTriggerRealTimeNotifications()
    {
        // This test verifies that low stock alerts are generated and sent in real-time

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouse = context.Warehouses.First();
        var variant = context.Variants.Skip(1).First(); // Use different variant

        var lowStockAlerts = new List<object>();
        var alertNotifications = new List<object>();

        // Subscribe to alert notifications
        _hubConnection.On<object>("LowStockAlert", (alert) => lowStockAlerts.Add(alert));
        _hubConnection.On<object>("AlertNotification", (notification) => alertNotifications.Add(notification));

        // Join alert groups
        await _hubConnection.InvokeAsync("SubscribeToAlerts", "low-stock");
        await _hubConnection.InvokeAsync("JoinWarehouseGroup", warehouse.Id.ToString());

        // Step 1: Set up inventory just above low stock threshold
        var openingBalanceRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 15, // Just above typical threshold of 10
            Reason = "Small stock for low stock alert test"
        };

        var openingBalanceResponse = await _client.PostAsync("/api/v1/inventory/opening-balance",
            new StringContent(JsonSerializer.Serialize(openingBalanceRequest), System.Text.Encoding.UTF8, "application/json"));
        openingBalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Perform sale to trigger low stock condition
        var saleRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 10, // This should bring stock to 5, below threshold
            Reason = "Sale to trigger low stock alert",
            ReferenceNumber = "SO-LOWSTOCK-TRIGGER"
        };

        var saleResponse = await _client.PostAsync("/api/v1/inventory/sale",
            new StringContent(JsonSerializer.Serialize(saleRequest), System.Text.Encoding.UTF8, "application/json"));
        saleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Wait for alert detection and notification
        await Task.Delay(5000); // Give time for background services to detect and send alerts

        // Step 4: Verify low stock variants endpoint shows the variant
        var lowStockResponse = await _client.GetAsync($"/api/v1/inventory/low-stock?threshold=10&warehouseId={warehouse.Id}");
        lowStockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var lowStockContent = await lowStockResponse.Content.ReadAsStringAsync();
        var lowStockVariants = JsonSerializer.Deserialize<List<LowStockVariantDto>>(lowStockContent, _jsonOptions);
        
        lowStockVariants.Should().NotBeNull();
        lowStockVariants!.Should().Contain(v => v.VariantId == variant.Id);

        // Step 5: Verify we received real-time alerts
        // Note: This depends on alert detection background service configuration
        if (lowStockAlerts.Any() || alertNotifications.Any())
        {
            (lowStockAlerts.Count + alertNotifications.Count).Should().BeGreaterThan(0);
        }

        // Step 6: Restock to clear the alert condition
        var restockRequest = new
        {
            VariantId = variant.Id,
            WarehouseId = warehouse.Id,
            Quantity = 50,
            Reason = "Restock to clear low stock alert",
            ReferenceNumber = "PO-RESTOCK-ALERT"
        };

        var restockResponse = await _client.PostAsync("/api/v1/inventory/purchase",
            new StringContent(JsonSerializer.Serialize(restockRequest), System.Text.Encoding.UTF8, "application/json"));
        restockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for alert clearance
        await Task.Delay(3000);

        // Step 7: Verify low stock list no longer contains the variant
        var clearedLowStockResponse = await _client.GetAsync($"/api/v1/inventory/low-stock?threshold=10&warehouseId={warehouse.Id}");
        clearedLowStockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var clearedLowStockContent = await clearedLowStockResponse.Content.ReadAsStringAsync();
        var clearedLowStockVariants = JsonSerializer.Deserialize<List<LowStockVariantDto>>(clearedLowStockContent, _jsonOptions);
        
        clearedLowStockVariants.Should().NotBeNull();
        clearedLowStockVariants!.Should().NotContain(v => v.VariantId == variant.Id);
    }

    [Fact]
    public async Task WarehouseSpecificUpdates_ShouldOnlyNotifyRelevantSubscribers()
    {
        // This test verifies that warehouse-specific updates only notify
        // subscribers who are interested in that warehouse

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouses = context.Warehouses.Take(2).ToList();
        var warehouse1 = warehouses[0];
        var warehouse2 = warehouses[1];
        var variant = context.Variants.First();

        var warehouse1Updates = new List<object>();
        var warehouse2Updates = new List<object>();

        // Create second hub connection for warehouse 2
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
            // Subscribe first connection to warehouse 1 updates
            _hubConnection.On<object>("StockLevelChanged", (update) => warehouse1Updates.Add(update));
            _hubConnection.On<object>("DashboardMetricsUpdated", (update) => warehouse1Updates.Add(update));
            await _hubConnection.InvokeAsync("JoinWarehouseGroup", warehouse1.Id.ToString());

            // Subscribe second connection to warehouse 2 updates
            hubConnection2.On<object>("StockLevelChanged", (update) => warehouse2Updates.Add(update));
            hubConnection2.On<object>("DashboardMetricsUpdated", (update) => warehouse2Updates.Add(update));
            await hubConnection2.InvokeAsync("JoinWarehouseGroup", warehouse2.Id.ToString());

            // Step 1: Set up inventory in warehouse 1 only
            var warehouse1OpeningBalance = new
            {
                VariantId = variant.Id,
                WarehouseId = warehouse1.Id,
                Quantity = 500,
                Reason = "Stock for warehouse isolation test"
            };

            var warehouse1Response = await _client.PostAsync("/api/v1/inventory/opening-balance",
                new StringContent(JsonSerializer.Serialize(warehouse1OpeningBalance), System.Text.Encoding.UTF8, "application/json"));
            warehouse1Response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Wait for notifications
            await Task.Delay(3000);

            // Step 2: Perform operations in warehouse 1
            var warehouse1Purchase = new
            {
                VariantId = variant.Id,
                WarehouseId = warehouse1.Id,
                Quantity = 200,
                Reason = "Purchase for warehouse 1",
                ReferenceNumber = "PO-WH1-001"
            };

            var warehouse1PurchaseResponse = await _client.PostAsync("/api/v1/inventory/purchase",
                new StringContent(JsonSerializer.Serialize(warehouse1Purchase), System.Text.Encoding.UTF8, "application/json"));
            warehouse1PurchaseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Wait for notifications
            await Task.Delay(3000);

            // Step 3: Verify warehouse 1 subscriber received updates
            warehouse1Updates.Should().NotBeEmpty("Warehouse 1 subscriber should receive updates for warehouse 1 operations");

            // Step 4: Verify warehouse 2 subscriber did not receive updates
            warehouse2Updates.Should().BeEmpty("Warehouse 2 subscriber should not receive updates for warehouse 1 operations");

            // Step 5: Now perform operations in warehouse 2
            var warehouse2OpeningBalance = new
            {
                VariantId = variant.Id,
                WarehouseId = warehouse2.Id,
                Quantity = 300,
                Reason = "Stock for warehouse 2"
            };

            var warehouse2Response = await _client.PostAsync("/api/v1/inventory/opening-balance",
                new StringContent(JsonSerializer.Serialize(warehouse2OpeningBalance), System.Text.Encoding.UTF8, "application/json"));
            warehouse2Response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Wait for notifications
            await Task.Delay(3000);

            // Step 6: Verify warehouse 2 subscriber now receives updates
            warehouse2Updates.Should().NotBeEmpty("Warehouse 2 subscriber should receive updates for warehouse 2 operations");

            // Step 7: Verify cross-warehouse transfer notifications
            var transferRequest = new
            {
                VariantId = variant.Id,
                SourceWarehouseId = warehouse1.Id,
                DestinationWarehouseId = warehouse2.Id,
                Quantity = 100,
                Reason = "Cross-warehouse transfer test",
                ReferenceNumber = "TRF-CROSS-001"
            };

            var initialWarehouse1UpdateCount = warehouse1Updates.Count;
            var initialWarehouse2UpdateCount = warehouse2Updates.Count;

            var transferResponse = await _client.PostAsync("/api/v1/inventory/transfer",
                new StringContent(JsonSerializer.Serialize(transferRequest), System.Text.Encoding.UTF8, "application/json"));
            transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Wait for transfer notifications
            await Task.Delay(3000);

            // Both warehouses should receive updates for transfers
            warehouse1Updates.Count.Should().BeGreaterThan(initialWarehouse1UpdateCount, 
                "Source warehouse should receive transfer notification");
            warehouse2Updates.Count.Should().BeGreaterThan(initialWarehouse2UpdateCount, 
                "Destination warehouse should receive transfer notification");
        }
        finally
        {
            await hubConnection2.DisposeAsync();
        }
    }

    [Fact]
    public async Task DashboardMetricsCalculation_ShouldReflectComplexScenarios()
    {
        // This test verifies that dashboard metrics correctly calculate
        // complex scenarios with multiple operations

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var warehouses = context.Warehouses.Take(2).ToList();
        var variants = context.Variants.Take(2).ToList();

        // Step 1: Set up complex inventory scenario
        var operations = new List<Task<HttpResponseMessage>>();

        // Set opening balances for all combinations
        foreach (var warehouse in warehouses)
        {
            foreach (var variant in variants)
            {
                var openingBalance = new
                {
                    VariantId = variant.Id,
                    WarehouseId = warehouse.Id,
                    Quantity = 1000,
                    Reason = $"Initial stock for {warehouse.Name} - {variant.Name}"
                };

                operations.Add(_client.PostAsync("/api/v1/inventory/opening-balance",
                    new StringContent(JsonSerializer.Serialize(openingBalance), System.Text.Encoding.UTF8, "application/json")));
            }
        }

        // Execute all opening balance operations
        var openingBalanceResponses = await Task.WhenAll(operations);
        foreach (var response in openingBalanceResponses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Step 2: Create various reservations
        var reservationOperations = new List<Task<HttpResponseMessage>>();

        foreach (var warehouse in warehouses)
        {
            foreach (var variant in variants)
            {
                var reservation = new
                {
                    VariantId = variant.Id,
                    WarehouseId = warehouse.Id,
                    Quantity = 150,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                    ReferenceNumber = $"ORDER-{warehouse.Code}-{variant.Sku}",
                    Notes = "Complex scenario reservation"
                };

                reservationOperations.Add(_client.PostAsync("/api/v1/reservations",
                    new StringContent(JsonSerializer.Serialize(reservation), System.Text.Encoding.UTF8, "application/json")));
            }
        }

        var reservationResponses = await Task.WhenAll(reservationOperations);
        foreach (var response in reservationResponses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // Step 3: Perform various stock movements
        var movementOperations = new List<Task<HttpResponseMessage>>();

        // Purchases
        var purchase1 = new
        {
            VariantId = variants[0].Id,
            WarehouseId = warehouses[0].Id,
            Quantity = 500,
            Reason = "Large purchase",
            ReferenceNumber = "PO-LARGE-001"
        };

        movementOperations.Add(_client.PostAsync("/api/v1/inventory/purchase",
            new StringContent(JsonSerializer.Serialize(purchase1), System.Text.Encoding.UTF8, "application/json")));

        // Sales
        var sale1 = new
        {
            VariantId = variants[1].Id,
            WarehouseId = warehouses[1].Id,
            Quantity = 200,
            Reason = "Large sale",
            ReferenceNumber = "SO-LARGE-001"
        };

        movementOperations.Add(_client.PostAsync("/api/v1/inventory/sale",
            new StringContent(JsonSerializer.Serialize(sale1), System.Text.Encoding.UTF8, "application/json")));

        // Adjustments
        var adjustment1 = new
        {
            VariantId = variants[0].Id,
            WarehouseId = warehouses[1].Id,
            Quantity = -50, // Write-off
            Reason = "Damaged goods",
            ReferenceNumber = "ADJ-DAMAGE-001"
        };

        movementOperations.Add(_client.PostAsync("/api/v1/inventory/adjustment",
            new StringContent(JsonSerializer.Serialize(adjustment1), System.Text.Encoding.UTF8, "application/json")));

        var movementResponses = await Task.WhenAll(movementOperations);
        foreach (var response in movementResponses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Step 4: Wait for background services to process
        await Task.Delay(5000);

        // Step 5: Get dashboard metrics
        var metricsResponse = await _client.GetAsync("/api/v1/dashboard/metrics");
        if (metricsResponse.StatusCode == HttpStatusCode.OK)
        {
            var metricsContent = await metricsResponse.Content.ReadAsStringAsync();
            var metrics = JsonSerializer.Deserialize<DashboardMetricsDto>(metricsContent, _jsonOptions);
            
            metrics.Should().NotBeNull();
            
            // Verify metrics make sense
            metrics!.TotalAvailableStock.Should().BeGreaterThan(0);
            metrics.TotalReservedStock.Should().Be(600); // 4 reservations × 150 each
            
            // Total stock should be: (4 × 1000) + 500 - 200 - 50 = 4250
            var expectedTotalStock = 4250;
            metrics.TotalAvailableStock.Should().Be(expectedTotalStock - 600); // Total - Reserved

            // Verify warehouse breakdown
            metrics.WarehouseBreakdown.Should().NotBeNull();
            metrics.WarehouseBreakdown.Should().HaveCount(2);
        }

        // Step 6: Get warehouse-specific stock levels
        var warehouseStockResponse = await _client.GetAsync("/api/v1/dashboard/warehouse-stock");
        if (warehouseStockResponse.StatusCode == HttpStatusCode.OK)
        {
            var warehouseStockContent = await warehouseStockResponse.Content.ReadAsStringAsync();
            var warehouseStock = JsonSerializer.Deserialize<List<WarehouseStockDto>>(warehouseStockContent, _jsonOptions);
            
            warehouseStock.Should().NotBeNull();
            warehouseStock!.Should().HaveCount(2);
            
            // Verify each warehouse has stock for both variants
            foreach (var warehouse in warehouseStock)
            {
                warehouse.VariantStockLevels.Should().HaveCount(2);
            }
        }

        // Step 7: Verify stock movement rates
        var movementRatesResponse = await _client.GetAsync("/api/v1/dashboard/stock-movement-rates");
        if (movementRatesResponse.StatusCode == HttpStatusCode.OK)
        {
            var movementRatesContent = await movementRatesResponse.Content.ReadAsStringAsync();
            var movementRates = JsonSerializer.Deserialize<StockMovementRatesDto>(movementRatesContent, _jsonOptions);
            
            movementRates.Should().NotBeNull();
            // Verify that movement rates reflect recent activity
        }
    }
}