# Inventory Management System (IMS) API

A production-grade, scalable inventory management system built with .NET Core 10 using Clean Architecture principles.

## Project Structure

```
src/
├── IMS.Api.Presentation/     # Web API layer (Controllers, Middleware, SignalR Hubs)
├── IMS.Api.Application/       # Application layer (Use Cases, Commands, Queries)
├── IMS.Api.Domain/           # Domain layer (Entities, Value Objects, Domain Services)
└── IMS.Api.Infrastructure/   # Infrastructure layer (Data Access, External Services)
```

## Architecture

The project follows Clean Architecture principles with clear separation of concerns:

- **Domain Layer**: Contains business entities, value objects, and domain logic
- **Application Layer**: Contains use cases, command/query handlers, and application services
- **Infrastructure Layer**: Contains data access, external service integrations, and infrastructure concerns
- **Presentation Layer**: Contains API controllers, middleware, and SignalR hubs

## Features

- **Clean Architecture**: Separation of concerns with dependency inversion
- **Entity Framework Core**: SQL Server database with Code First migrations
- **JWT Authentication**: Secure token-based authentication
- **Role-based Authorization**: Granular permission system
- **SignalR**: Real-time dashboard updates
- **CORS Support**: Cross-origin resource sharing
- **Error Handling**: Global exception handling middleware
- **Logging**: Structured logging with multiple providers

## Configuration

### Database Connection

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=IMS_Database;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### JWT Settings

Configure JWT authentication in `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "IMS.Api",
    "Audience": "IMS.Api.Users",
    "ExpirationInMinutes": 60
  }
}
```

## Getting Started

1. **Clone the repository**
2. **Restore packages**: `dotnet restore`
3. **Update database**: `dotnet ef database update` (from Infrastructure project)
4. **Run the application**: `dotnet run --project src/IMS.Api.Presentation`

## API Endpoints

- **Health Check**: `GET /api/v1/health`
- **Swagger UI**: Available in development mode at `/swagger`

## Authorization Policies

- `RequireWarehouseManager`: For warehouse management operations
- `RequireInventoryAnalyst`: For inventory analysis and reporting
- `RequireWarehouseOperator`: For basic warehouse operations

## Next Steps

This is the foundational setup. The following tasks will implement:

1. Core domain models and value objects
2. Database schema and EF Core configuration
3. CRUD services for basic entities
4. Inventory domain services with commands and events
5. Query handlers for inventory and dashboard data
6. RESTful API controllers
7. SignalR hubs for real-time functionality
8. Authentication and authorization
9. Audit trail and compliance features
10. Performance optimizations and caching
11. Integration and end-to-end testing