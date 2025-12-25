# IMS API Integration Tests

This project contains comprehensive integration tests for the Inventory Management System (IMS) API.

## Test Structure

### Infrastructure
- `IntegrationTestWebApplicationFactory<T>` - Sets up test environment with PostgreSQL test containers
- `TestDataSeeder` - Seeds test data for consistent test scenarios
- `AuthenticationHelper` - Handles JWT authentication for test clients

### Test Categories

#### API Integration Tests (`Controllers/`)
- `StockMovementWorkflowTests` - Tests complete stock movement workflows
- `ReservationWorkflowTests` - Tests reservation creation, modification, and cancellation
- `MultiTenantIsolationTests` - Verifies tenant data isolation

#### SignalR Integration Tests (`SignalR/`)
- `DashboardHubTests` - Tests real-time dashboard notifications and hub connections

#### End-to-End Tests (`EndToEnd/`)
- `InventoryManagementWorkflowTests` - Complete inventory management scenarios
- `ReservationAndRefundWorkflowTests` - Order fulfillment and refund processes
- `DashboardRealTimeUpdatesTests` - Real-time dashboard updates and alerts

## Test Features

### Comprehensive Coverage
- **Stock Movement Workflows**: Opening balance, purchases, sales, adjustments, transfers
- **Reservation Management**: Creation, modification, cancellation, expiry handling
- **Multi-tenant Isolation**: Ensures tenant data separation and security
- **Real-time Updates**: SignalR hub notifications and dashboard metrics
- **Error Scenarios**: Out-of-stock, insufficient inventory, validation errors

### Test Infrastructure
- **PostgreSQL Test Containers**: Isolated database for each test run
- **JWT Authentication**: Realistic authentication scenarios
- **Test Data Seeding**: Consistent test data across scenarios
- **SignalR Testing**: Real-time communication testing

## Running Tests

```bash
# Build the test project
dotnet build tests/IMS.Api.IntegrationTests/IMS.Api.IntegrationTests.csproj

# Run all tests
dotnet test tests/IMS.Api.IntegrationTests/IMS.Api.IntegrationTests.csproj

# Run specific test category
dotnet test tests/IMS.Api.IntegrationTests/IMS.Api.IntegrationTests.csproj --filter "Category=StockMovement"

# Run with detailed output
dotnet test tests/IMS.Api.IntegrationTests/IMS.Api.IntegrationTests.csproj --logger "console;verbosity=detailed"
```

## Test Requirements

### Prerequisites
- .NET 10 SDK
- Docker (for PostgreSQL test containers)
- PostgreSQL test container support

### Dependencies
- xUnit for test framework
- FluentAssertions for readable assertions
- Testcontainers.PostgreSql for database isolation
- Microsoft.AspNetCore.Mvc.Testing for API testing
- Microsoft.AspNetCore.SignalR.Client for SignalR testing

## Test Scenarios

### Stock Movement Integration Tests
1. **Complete Stock Movement Workflow**
   - Set opening balance
   - Record purchases and sales
   - Perform stock adjustments
   - Execute warehouse transfers
   - Verify inventory consistency

2. **Error Handling**
   - Out-of-stock scenarios
   - Duplicate opening balance attempts
   - Invalid stock movements

### Reservation Integration Tests
1. **Reservation Lifecycle**
   - Create reservations
   - Modify quantities and expiry dates
   - Cancel reservations
   - Handle reservation expiry

2. **Multi-item Orders**
   - Partial fulfillment scenarios
   - Backorder handling
   - Complex reservation management

### Multi-tenant Isolation Tests
1. **Data Separation**
   - Tenant-specific product access
   - Isolated inventory data
   - Separate reservation management
   - Cross-tenant access prevention

### Real-time Dashboard Tests
1. **SignalR Hub Communication**
   - Connection management
   - Group subscriptions
   - Real-time notifications

2. **Dashboard Metrics**
   - Stock level changes
   - Low stock alerts
   - Warehouse-specific updates

## Notes

- Tests use PostgreSQL test containers for database isolation
- Each test class has its own database instance
- Authentication is handled through JWT tokens
- SignalR tests verify real-time communication
- All tests are designed to be independent and repeatable

The test suite provides comprehensive coverage of the IMS API functionality, ensuring reliability and correctness of the inventory management system.