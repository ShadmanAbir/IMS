# IMS API Project Structure

## Solution Overview

```
IMS.Api.sln
├── src/
│   ├── IMS.Api.Presentation/          # Web API Layer
│   │   ├── Controllers/
│   │   │   └── HealthController.cs    # Health check endpoint
│   │   ├── Middleware/
│   │   │   └── ErrorHandlingMiddleware.cs  # Global error handling
│   │   ├── Program.cs                 # Application entry point
│   │   ├── appsettings.json          # Configuration
│   │   └── IMS.Api.Presentation.csproj
│   │
│   ├── IMS.Api.Application/           # Application Layer
│   │   ├── Common/
│   │   │   └── Interfaces/
│   │   │       ├── IRepository.cs     # Generic repository interface
│   │   │       └── IUnitOfWork.cs     # Unit of work pattern
│   │   └── IMS.Api.Application.csproj
│   │
│   ├── IMS.Api.Domain/               # Domain Layer
│   │   ├── Common/
│   │   │   ├── AggregateRoot.cs      # Base aggregate root
│   │   │   ├── Entity.cs             # Base entity
│   │   │   ├── ValueObject.cs        # Base value object
│   │   │   ├── IDomainEvent.cs       # Domain event interface
│   │   │   └── Result.cs             # Result pattern
│   │   └── IMS.Api.Domain.csproj
│   │
│   └── IMS.Api.Infrastructure/       # Infrastructure Layer
│       ├── Data/
│       │   └── ApplicationDbContext.cs  # EF Core DbContext
│       ├── Repositories/
│       │   └── Repository.cs         # Generic repository implementation
│       └── IMS.Api.Infrastructure.csproj
│
├── README.md                         # Project documentation
├── PROJECT_STRUCTURE.md             # This file
└── .kiro/                           # Kiro specifications
    └── specs/
        └── ims-api-realtime-dashboard/
            ├── requirements.md
            ├── design.md
            └── tasks.md
```

## Layer Dependencies

```
Presentation → Application → Domain
     ↓              ↓
Infrastructure → Application
```

## Key Features Implemented

### ✅ Clean Architecture Setup
- [x] Domain layer with base classes (Entity, AggregateRoot, ValueObject)
- [x] Application layer with interfaces (IRepository, IUnitOfWork)
- [x] Infrastructure layer with EF Core setup
- [x] Presentation layer with Web API configuration

### ✅ Core Infrastructure
- [x] .NET Core 10 Web API project
- [x] Entity Framework Core with SQL Server provider
- [x] JWT Authentication middleware configured
- [x] Authorization policies for different roles
- [x] SignalR support configured
- [x] CORS support
- [x] Global error handling middleware
- [x] Structured logging
- [x] Swagger/OpenAPI documentation

### ✅ Configuration
- [x] Connection string management
- [x] JWT settings configuration
- [x] Environment-specific settings
- [x] Dependency injection container setup

### ✅ Testing & Validation
- [x] Solution builds successfully
- [x] Application starts and runs
- [x] Health endpoint responds correctly
- [x] All project references work correctly

## Next Implementation Steps

The foundation is now ready for implementing the core business logic:

1. **Domain Models** - Product, Variant, InventoryItem entities
2. **Value Objects** - SKU, UnitOfMeasure, ProductId, etc.
3. **Database Schema** - EF Core configurations and migrations
4. **Business Services** - Command and query handlers
5. **API Controllers** - RESTful endpoints
6. **Real-time Features** - SignalR hubs for dashboard updates
7. **Authentication** - JWT token generation and validation
8. **Testing** - Unit and integration tests

## Configuration Notes

- **Database**: Uses LocalDB by default, easily configurable for other SQL Server instances
- **JWT**: Configured with role-based authorization policies
- **CORS**: Currently allows all origins (should be restricted in production)
- **Logging**: Console and debug providers configured
- **Error Handling**: Global middleware catches and formats exceptions

This foundation provides a solid, scalable base for implementing the complete Inventory Management System according to the Clean Architecture principles.