# Design Document

## Overview

The Inventory Management System (IMS) is designed as a production-grade, scalable system built on .NET Core 10 using Clean Architecture principles. The system provides comprehensive inventory management capabilities with real-time dashboard functionality through SignalR, strong consistency guarantees, and complete audit trails.

The architecture emphasizes domain-driven design (DDD) with clear bounded contexts, CQRS patterns for read/write separation, and event-driven communication for real-time updates. The system maintains strict data integrity through immutable stock movements and decimal-based calculations while providing sub-100ms query performance.

## Architecture

### Clean Architecture Layers

```
┌─────────────────────────────────────────┐
│           Presentation Layer            │
│  ┌─────────────────┐ ┌─────────────────┐│
│  │   Web API       │ │   SignalR Hubs  ││
│  │   Controllers   │ │   Real-time     ││
│  └─────────────────┘ └─────────────────┘│
└─────────────────────────────────────────┘
┌─────────────────────────────────────────┐
│           Application Layer             │
│  ┌─────────────────┐ ┌─────────────────┐│
│  │   Use Cases     │ │   Query         ││
│  │   Commands      │ │   Handlers      ││
│  └─────────────────┘ └─────────────────┘│
└─────────────────────────────────────────┘
┌─────────────────────────────────────────┐
│             Domain Layer                │
│  ┌─────────────────┐ ┌─────────────────┐│
│  │   Entities      │ │   Domain        ││
│  │   Aggregates    │ │   Services      ││
│  └─────────────────┘ └─────────────────┘│
└─────────────────────────────────────────┘
┌─────────────────────────────────────────┐
│          Infrastructure Layer           │
│  ┌─────────────────┐ ┌─────────────────┐│
│  │   EF Core       │ │   External      ││
│  │   Repositories  │ │   Services      ││
│  └─────────────────┘ └─────────────────┘│
└─────────────────────────────────────────┘
```

### Bounded Contexts

1. **Product Management Context**: Products, variants, attributes, categories
2. **Inventory Context**: Stock levels, movements, reservations
3. **Warehouse Context**: Locations, transfers, warehouse-specific operations
4. **Pricing Context**: Base pricing, tiers, customer groups
5. **Analytics Context**: Real-time metrics, dashboards, alerts
6. **Audit Context**: Historical data, compliance, reporting

### CQRS Implementation

- **Command Side**: Handles all write operations with strong consistency
- **Query Side**: Optimized read models for fast queries and dashboard data
- **Event Store**: Immutable stock movements serve as event sourcing foundation
- **Read Models**: Materialized views for dashboard metrics and bulk queries

## Components and Interfaces

### Core Domain Entities

#### Product Aggregate
```csharp
public class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public List<ProductAttribute> Attributes { get; private set; }
    public List<Variant> Variants { get; private set; }
    public TenantId TenantId { get; private set; }
}

public class Variant : Entity<VariantId>
{
    public SKU Sku { get; private set; } // Immutable
    public string Name { get; private set; }
    public UnitOfMeasure BaseUnit { get; private set; }
    public List<VariantAttribute> Attributes { get; private set; }
    public List<UnitConversion> UnitConversions { get; private set; }
}
```

#### Inventory Aggregate
```csharp
public class InventoryItem : AggregateRoot<InventoryItemId>
{
    public VariantId VariantId { get; private set; }
    public WarehouseId WarehouseId { get; private set; }
    public decimal TotalStock { get; private set; } // Base units only
    public decimal ReservedStock { get; private set; }
    public decimal AvailableStock => TotalStock - ReservedStock;
    public List<StockMovement> Movements { get; private set; }
    public List<Reservation> Reservations { get; private set; }
    public bool AllowNegativeStock { get; private set; }
}

public class StockMovement : Entity<StockMovementId>
{
    public MovementType Type { get; private set; }
    public decimal Quantity { get; private set; } // Base units, can be negative
    public decimal RunningBalance { get; private set; }
    public string Reason { get; private set; }
    public UserId ActorId { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public string ReferenceNumber { get; private set; }
    public MovementMetadata Metadata { get; private set; }
}
```

#### Reservation Aggregate
```csharp
public class Reservation : AggregateRoot<ReservationId>
{
    public VariantId VariantId { get; private set; }
    public WarehouseId WarehouseId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public ReservationStatus Status { get; private set; }
    public string ReferenceNumber { get; private set; }
    public UserId CreatedBy { get; private set; }
}
```

### Application Services

#### Command Handlers
```csharp
public interface IStockMovementCommandHandler
{
    Task<Result> HandleOpeningBalanceAsync(CreateOpeningBalanceCommand command);
    Task<Result> HandlePurchaseAsync(RecordPurchaseCommand command);
    Task<Result> HandleSaleAsync(RecordSaleCommand command);
    Task<Result> HandleAdjustmentAsync(RecordAdjustmentCommand command);
    Task<Result> HandleTransferAsync(RecordTransferCommand command);
    Task<Result> HandleRefundAsync(ProcessRefundCommand command);
    Task<Result> HandleWriteOffAsync(RecordWriteOffCommand command);
}

public interface IReservationCommandHandler
{
    Task<Result<ReservationId>> CreateReservationAsync(CreateReservationCommand command);
    Task<Result> ModifyReservationAsync(ModifyReservationCommand command);
    Task<Result> CancelReservationAsync(CancelReservationCommand command);
    Task<Result> ExpireReservationsAsync(ExpireReservationsCommand command);
}
```

#### Query Handlers
```csharp
public interface IInventoryQueryHandler
{
    Task<InventoryLevelDto> GetInventoryLevelAsync(VariantId variantId, WarehouseId warehouseId);
    Task<List<InventoryLevelDto>> GetBulkInventoryLevelsAsync(BulkInventoryQuery query);
    Task<PagedResult<StockMovementDto>> GetStockMovementHistoryAsync(StockMovementQuery query);
    Task<List<LowStockVariantDto>> GetLowStockVariantsAsync(LowStockQuery query);
}

public interface IDashboardQueryHandler
{
    Task<DashboardMetricsDto> GetRealTimeMetricsAsync(DashboardQuery query);
    Task<List<WarehouseStockDto>> GetWarehouseStockLevelsAsync(WarehouseStockQuery query);
    Task<StockMovementRatesDto> GetStockMovementRatesAsync(MovementRatesQuery query);
    Task<List<AlertDto>> GetActiveAlertsAsync(AlertQuery query);
}
```

### Infrastructure Components

#### Repository Interfaces
```csharp
public interface IInventoryRepository : IRepository<InventoryItem>
{
    Task<InventoryItem> GetByVariantAndWarehouseAsync(VariantId variantId, WarehouseId warehouseId);
    Task<List<InventoryItem>> GetBulkInventoryAsync(List<VariantId> variantIds, WarehouseId warehouseId);
    Task<bool> HasOpeningBalanceAsync(VariantId variantId, WarehouseId warehouseId);
}

public interface IStockMovementRepository : IRepository<StockMovement>
{
    Task<PagedResult<StockMovement>> GetMovementHistoryAsync(StockMovementQuery query);
    Task<List<StockMovement>> GetMovementsByReferenceAsync(string referenceNumber);
}
```

#### SignalR Hubs
```csharp
public class DashboardHub : Hub
{
    public async Task JoinWarehouseGroup(string warehouseId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"warehouse-{warehouseId}");
    }

    public async Task JoinVariantGroup(string variantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"variant-{variantId}");
    }

    public async Task SubscribeToAlerts(string alertType)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"alerts-{alertType}");
    }
}

public interface IDashboardNotificationService
{
    Task NotifyStockLevelChangeAsync(VariantId variantId, WarehouseId warehouseId, StockLevelChangeDto change);
    Task NotifyLowStockAlertAsync(LowStockAlertDto alert);
    Task NotifyReservationExpiryAsync(ReservationExpiryAlertDto alert);
    Task NotifyUnusualAdjustmentAsync(UnusualAdjustmentAlertDto alert);
}
```

## Data Models

### Database Schema Design

#### Core Tables
```sql
-- Products and Variants
CREATE TABLE Products (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    CategoryId UNIQUEIDENTIFIER,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    UpdatedAtUtc DATETIME2 NOT NULL
);

CREATE TABLE Variants (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    SKU NVARCHAR(100) NOT NULL UNIQUE,
    Name NVARCHAR(255) NOT NULL,
    BaseUnit NVARCHAR(50) NOT NULL,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    UpdatedAtUtc DATETIME2 NOT NULL,
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

-- Inventory and Stock Movements
CREATE TABLE InventoryItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    VariantId UNIQUEIDENTIFIER NOT NULL,
    WarehouseId UNIQUEIDENTIFIER NOT NULL,
    TotalStock DECIMAL(18,6) NOT NULL,
    ReservedStock DECIMAL(18,6) NOT NULL DEFAULT 0,
    AllowNegativeStock BIT NOT NULL DEFAULT 0,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    UpdatedAtUtc DATETIME2 NOT NULL,
    UNIQUE (VariantId, WarehouseId, TenantId),
    FOREIGN KEY (VariantId) REFERENCES Variants(Id),
    FOREIGN KEY (WarehouseId) REFERENCES Warehouses(Id)
);

CREATE TABLE StockMovements (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    InventoryItemId UNIQUEIDENTIFIER NOT NULL,
    MovementType NVARCHAR(50) NOT NULL,
    Quantity DECIMAL(18,6) NOT NULL,
    RunningBalance DECIMAL(18,6) NOT NULL,
    Reason NVARCHAR(255),
    ActorId UNIQUEIDENTIFIER NOT NULL,
    TimestampUtc DATETIME2 NOT NULL,
    ReferenceNumber NVARCHAR(100),
    Metadata NVARCHAR(MAX), -- JSON
    TenantId UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems(Id)
);

-- Reservations
CREATE TABLE Reservations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    VariantId UNIQUEIDENTIFIER NOT NULL,
    WarehouseId UNIQUEIDENTIFIER NOT NULL,
    Quantity DECIMAL(18,6) NOT NULL,
    ExpiresAtUtc DATETIME2 NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    ReferenceNumber NVARCHAR(100),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY (VariantId) REFERENCES Variants(Id),
    FOREIGN KEY (WarehouseId) REFERENCES Warehouses(Id)
);
```

#### Read Model Tables (Optimized for Queries)
```sql
-- Dashboard Metrics (Materialized View)
CREATE TABLE DashboardMetrics (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    WarehouseId UNIQUEIDENTIFIER,
    VariantId UNIQUEIDENTIFIER,
    TotalStock DECIMAL(18,6),
    AvailableStock DECIMAL(18,6),
    ReservedStock DECIMAL(18,6),
    LowStockThreshold DECIMAL(18,6),
    IsLowStock BIT,
    IsOutOfStock BIT,
    LastMovementUtc DATETIME2,
    UpdatedAtUtc DATETIME2 NOT NULL,
    TenantId UNIQUEIDENTIFIER NOT NULL
);

-- Stock Movement Rates (Aggregated)
CREATE TABLE StockMovementRates (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    VariantId UNIQUEIDENTIFIER NOT NULL,
    WarehouseId UNIQUEIDENTIFIER,
    MovementType NVARCHAR(50) NOT NULL,
    PeriodStart DATETIME2 NOT NULL,
    PeriodEnd DATETIME2 NOT NULL,
    TotalQuantity DECIMAL(18,6) NOT NULL,
    MovementCount INT NOT NULL,
    AverageQuantity DECIMAL(18,6) NOT NULL,
    TenantId UNIQUEIDENTIFIER NOT NULL
);
```

### API Response Models

#### Inventory DTOs
```csharp
public class InventoryLevelDto
{
    public Guid VariantId { get; set; }
    public string SKU { get; set; }
    public string VariantName { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; }
    public decimal TotalStock { get; set; }
    public decimal ReservedStock { get; set; }
    public decimal AvailableStock { get; set; }
    public string BaseUnit { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}

public class StockMovementDto
{
    public Guid Id { get; set; }
    public string MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal RunningBalance { get; set; }
    public string Reason { get; set; }
    public string ActorName { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string ReferenceNumber { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

#### Dashboard DTOs
```csharp
public class DashboardMetricsDto
{
    public decimal TotalStockValue { get; set; }
    public decimal TotalAvailableStock { get; set; }
    public decimal TotalReservedStock { get; set; }
    public int LowStockVariantCount { get; set; }
    public int OutOfStockVariantCount { get; set; }
    public List<WarehouseStockDto> WarehouseBreakdown { get; set; }
    public StockMovementRatesDto MovementRates { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
}

public class RealTimeEventDto
{
    public string EventType { get; set; }
    public Guid VariantId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Dictionary<string, object> Data { get; set; }
    public DateTime TimestampUtc { get; set; }
}
```

## Error Handling

### Structured Error Codes

```csharp
public static class ErrorCodes
{
    // Stock Management Errors
    public const string OUT_OF_STOCK = "OUT_OF_STOCK";
    public const string INSUFFICIENT_STOCK = "INSUFFICIENT_STOCK";
    public const string NEGATIVE_STOCK_NOT_ALLOWED = "NEGATIVE_STOCK_NOT_ALLOWED";
    
    // Unit and Conversion Errors
    public const string INVALID_UNIT = "INVALID_UNIT";
    public const string UNIT_CONVERSION_NOT_FOUND = "UNIT_CONVERSION_NOT_FOUND";
    public const string INVALID_QUANTITY = "INVALID_QUANTITY";
    
    // SKU and Product Errors
    public const string DUPLICATE_SKU = "DUPLICATE_SKU";
    public const string VARIANT_NOT_FOUND = "VARIANT_NOT_FOUND";
    public const string PRODUCT_NOT_FOUND = "PRODUCT_NOT_FOUND";
    
    // Opening Balance Errors
    public const string OPENING_BALANCE_EXISTS = "OPENING_BALANCE_EXISTS";
    public const string OPENING_BALANCE_REQUIRED = "OPENING_BALANCE_REQUIRED";
    
    // Refund Errors
    public const string REFUND_EXCEEDS_SALE = "REFUND_EXCEEDS_SALE";
    public const string ORIGINAL_SALE_NOT_FOUND = "ORIGINAL_SALE_NOT_FOUND";
    public const string REFUND_ALREADY_PROCESSED = "REFUND_ALREADY_PROCESSED";
    
    // Reservation Errors
    public const string RESERVATION_EXPIRED = "RESERVATION_EXPIRED";
    public const string RESERVATION_NOT_FOUND = "RESERVATION_NOT_FOUND";
    public const string RESERVATION_ALREADY_USED = "RESERVATION_ALREADY_USED";
    
    // Warehouse Errors
    public const string WAREHOUSE_NOT_FOUND = "WAREHOUSE_NOT_FOUND";
    public const string INVALID_WAREHOUSE_TRANSFER = "INVALID_WAREHOUSE_TRANSFER";
    
    // Authentication and Authorization
    public const string UNAUTHORIZED = "UNAUTHORIZED";
    public const string FORBIDDEN = "FORBIDDEN";
    public const string INVALID_TOKEN = "INVALID_TOKEN";
    
    // Validation Errors
    public const string VALIDATION_FAILED = "VALIDATION_FAILED";
    public const string REQUIRED_FIELD_MISSING = "REQUIRED_FIELD_MISSING";
    public const string INVALID_DATE_RANGE = "INVALID_DATE_RANGE";
}

public class ApiErrorResponse
{
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public Dictionary<string, object> Details { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string TraceId { get; set; }
}
```

### Error Handling Strategy

1. **Domain Validation**: Business rule violations return domain-specific error codes
2. **Infrastructure Errors**: Database and external service errors are wrapped with appropriate codes
3. **API Validation**: Input validation errors include field-specific details
4. **Idempotency**: Duplicate operations return success with original result
5. **Logging**: All errors logged with correlation IDs for tracing

## Testing Strategy

### Unit Testing
- **Domain Logic**: Test all business rules and calculations
- **Command Handlers**: Verify state changes and error conditions
- **Query Handlers**: Test data retrieval and filtering logic
- **Value Objects**: Validate immutability and equality

### Integration Testing
- **API Endpoints**: Test complete request/response cycles
- **Database Operations**: Verify EF Core mappings and queries
- **SignalR Hubs**: Test real-time message delivery
- **Authentication**: Validate JWT token handling

### Performance Testing
- **Query Performance**: Ensure sub-100ms response times
- **Bulk Operations**: Test large dataset handling
- **Concurrent Operations**: Verify thread safety and consistency
- **Real-time Updates**: Measure SignalR message delivery latency

### End-to-End Testing
- **Stock Movement Flows**: Complete inventory operation scenarios
- **Dashboard Updates**: Real-time metric calculation and delivery
- **Multi-tenant Isolation**: Verify tenant data separation
- **Error Scenarios**: Test error handling and recovery

The design emphasizes strong consistency, audit trails, and real-time capabilities while maintaining clean separation of concerns and testability throughout the system.