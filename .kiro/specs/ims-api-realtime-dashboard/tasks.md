# Implementation Plan

# Implementation Plan

- [x] 1. Set up project structure and core infrastructure
  - Create Clean Architecture solution structure with API, Application, Domain, and Infrastructure projects
  - Configure .NET Core 10 with dependency injection, logging, and configuration
  - Set up Dapper with SQL Server provider and connection string management (needs PostgreSQL migration)
  - Configure JWT authentication middleware and authorization policies
  - _Requirements: 12.1, 12.2, 13.1, 13.2_

- [x] 2. Implement core domain models and value objects
  - [x] 2.1 Create fundamental value objects (ProductId, VariantId, SKU, UnitOfMeasure)
    - Implement immutable value objects with proper equality and validation
    - Create SKU value object with uniqueness constraints and immutability rules
    - _Requirements: 1.3, 1.4, 12.3_

  - [x] 2.2 Implement Product and Variant aggregates
    - Create Product aggregate root with variant management capabilities
    - Implement Variant entity with base unit and attribute support
    - Add unit conversion system for weight, volume, count, and length
    - _Requirements: 1.1, 1.2, 1.5, 2.4_

  - [x] 2.3 Create InventoryItem aggregate with stock movement tracking
    - Implement InventoryItem aggregate root with stock calculations
    - Create StockMovement entity with immutable movement records
    - Add business rules for negative stock prevention and base unit enforcement
    - _Requirements: 5.1, 5.3, 5.5, 4.2_

  - [ ]* 2.4 Write unit tests for domain models
    - Test value object immutability and equality
    - Verify aggregate business rules and invariants
    - Test unit conversion calculations and edge cases
    - _Requirements: 1.3, 1.5, 5.5_

- [x] 3. **CRITICAL: Implement hybrid data access architecture and code cleanup**





  - [x] 3.1 **ARCHITECTURAL DECISION**: Implement EF Core + Dapper hybrid approach


    - **Write Operations**: Use EF Core with AutoMapper for all create, update, delete operations
    - **Read Operations**: Use Dapper for all queries, especially complex dashboard queries
    - Keep ApplicationDbContext and entity configurations for write operations
    - Maintain Dapper repositories for optimized read queries
    - Update dependency injection to support both approaches clearly
    - _Requirements: 12.1, 12.3_



  - [x] 3.2 Implement soft delete pattern with IsDeleted flag

    - Add IsDeleted property to all deletable entities (Product, Variant, InventoryItem, etc.)
    - Update EF Core configurations to include global query filters for soft deletes
    - Modify all delete operations to set IsDeleted = true instead of physical deletion
    - Update Dapper queries to filter out soft-deleted records
    - Add DeletedAtUtc and DeletedBy audit fields for soft delete tracking


    - _Requirements: 11.1, 11.4, 11.5_

  - [x] 3.3 Code review and cleanup - Remove unnecessary code and add proper comments

    - Review all existing code files for unused imports, dead code, and redundant implementations
    - Add comprehensive XML documentation comments to all public APIs
    - Remove any duplicate or conflicting implementations


    - Standardize coding patterns and naming conventions across the solution
    - Add inline comments for complex business logic and calculations
    - _Requirements: 12.1, 12.2_

  - [x] 3.4 Migrate from SQL Server to PostgreSQL with proper setup

    - Replace SqlServerConnectionFactory with PostgreSqlConnectionFactory using Npgsql
    - Update connection string configuration for PostgreSQL


    - Update all SQL queries in Dapper repositories to use PostgreSQL syntax
    - Install Npgsql and Npgsql.EntityFrameworkCore.PostgreSQL packages
    - Remove Microsoft.EntityFrameworkCore.SqlServer dependency
    - Update EF Core configurations for PostgreSQL-specific features
    - _Requirements: 12.1, 12.3_



  - [x] 3.5 Remove EF Core parameterless constructors from domain models

    - Remove parameterless constructors from Product, Variant, InventoryItem, StockMovement
    - Update EF Core entity configurations to work with constructor-based initialization
    - Ensure proper domain encapsulation and immutability

    - Test that EF Core can still materialize entities correctly
    - _Requirements: 12.3, 1.3, 1.4_

  - [x] 3.6 Implement double-entry stock movement system

    - Update StockMovement entity to support double-entry accounting principles
    - Create debit and credit entries for each stock transaction
    - Ensure stock movement balance validation and audit trail integrity
    - Update repository methods to handle double-entry creation
    - _Requirements: 5.1, 5.3, 11.1_

  - [x] 3.7 Add optional expiry date support for inventory products

    - Add ExpiryDate property to InventoryItem entity (nullable for non-perishable items)
    - Implement expiry date tracking and alerts in dashboard queries
    - Add FIFO/FEFO inventory management capabilities in stock movement logic
    - Update DTOs and API responses to include expiry information
    - _Requirements: 1.1, 1.2, 9.3_

  - [x] 3.8 Complete missing DTOs and mapping infrastructure


    - Create comprehensive DTOs for all domain entities (StockMovementDto, VariantDto, etc.)
    - Implement AutoMapper profiles for domain-to-DTO mapping
    - Add validation attributes to DTOs for API input validation
    - Ensure DTOs support the soft delete pattern (exclude IsDeleted entities)
    - _Requirements: 12.1, 12.2_

- [x] 4. Implement CQRS command/query pipeline with MediatR




  - [x] 4.1 Configure MediatR for command and query dispatch


    - Install and configure MediatR package in Application layer
    - Set up dependency injection for handlers
    - Create base command and query interfaces
    - _Requirements: 12.1, 12.2_


  - [x] 4.2 Implement pipeline behaviors

    - Create validation behavior using FluentValidation
    - Add logging behavior for command/query tracking
    - Implement transaction behavior for write operations
    - _Requirements: 14.1, 14.2, 11.1_

  - [x] 4.3 Create command handlers for stock movements


    - Implement OpeningBalanceCommandHandler with single-creation validation
    - Create PurchaseCommandHandler for stock increases
    - Build SaleCommandHandler with stock reduction and validation
    - Add AdjustmentCommandHandler for manual stock corrections
    - Implement TransferCommandHandler for warehouse-to-warehouse moves
    - _Requirements: 5.2, 5.4, 3.3, 3.5_


  - [x] 4.4 Create query handlers for inventory data

    - Implement GetInventoryLevelQueryHandler for single variant queries
    - Build GetBulkInventoryLevelsQueryHandler for multi-variant retrieval
    - Create GetStockMovementHistoryQueryHandler with pagination
    - Add GetLowStockVariantsQueryHandler with configurable thresholds
    - _Requirements: 4.1, 4.3, 4.4, 4.5, 11.2, 11.3_
-

- [x] 5. Create missing domain entities and services



  - [x] 5.1 Implement Warehouse aggregate


    - Create Warehouse aggregate root with location details
    - Add warehouse configuration and capacity management
    - _Requirements: 3.1, 3.4_

  - [x] 5.2 Implement Category entity


    - Create Category entity with hierarchical relationships
    - Add category management with parent-child relationships
    - _Requirements: 2.1, 2.2_

  - [x] 5.3 Create Reservation aggregate


    - Implement Reservation aggregate with expiry management
    - Add reservation modification and cancellation capabilities
    - Create background service for automatic reservation expiry
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

  - [x] 5.4 Implement Pricing entities


    - Create base pricing entities for variants
    - Add pricing tier and customer group management
    - Create unit-aware pricing calculations
    - _Requirements: 7.1, 7.2, 7.3, 7.4_



- [ ] 6. Implement refund processing with sale reference validation

  - [x] 6.1 Create RefundCommandHandler with original sale lookup


    - Validate refund quantities against original sale amounts
    - Generate stock movement records for refund transactions
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_
                              
- [x] 7. Create RESTful API controllers with proper versioning





  - [x] 7.1 Implement Product and Variant API controllers


    - Create ProductsController with CRUD endpoints
    - Build VariantsController with product relationship management
    - Add attribute and category management endpoints
    - _Requirements: 12.1, 2.1, 2.2, 2.5_

  - [x] 7.2 Create Inventory API controllers


    - Implement InventoryController for stock level queries
    - Build StockMovementsController for movement recording and history
    - Add bulk inventory query endpoints with filtering
    - _Requirements: 12.1, 4.1, 4.4, 5.1_


  - [x] 7.3 Implement Warehouse API controllers

    - Create WarehousesController for warehouse management
    - Add location and configuration management endpoints
    - _Requirements: 12.1, 3.1, 3.4_


  - [x] 7.4 Create Reservation API controllers

    - Implement ReservationsController for stock allocation management
    - Add reservation modification and cancellation endpoints
    - Implement reservation expiry monitoring endpoints
    - _Requirements: 12.1, 6.1, 6.4_



  - [ ] 7.5 Create Refund API controllers
    - Implement RefundsController for return processing
    - Add sale reference validation and partial refund support


    - _Requirements: 12.1, 8.1, 8.4_

  - [ ] 7.6 Add comprehensive API validation and error handling
    - Implement model validation with FluentValidation
    - Create structured error response middleware


    - Add idempotency support for write operations
    - Configure proper HTTP status codes and error messages
    - _Requirements: 12.2, 14.1, 14.2, 14.3, 14.4, 14.5_

- [-] 8. Implement dashboard query handlers for real-time metrics



  - [x] 8.1 Create dashboard metrics query handlers



    - Create GetRealTimeMetricsQueryHandler for dashboard overview
    - Build GetWarehouseStockLevelsQueryHandler for location-specific data
    - Implement GetStockMovementRatesQueryHandler for trend analysis
    - Add GetActiveAlertsQueryHandler for operational notifications




    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

  - [ ]* 8.2 Write unit tests for query handlers
    - Test query filtering and pagination logic


    - Verify dashboard metric calculations
    - Test performance with large datasets
    - _Requirements: 4.5, 9.5, 11.3_


- [x] 9. Implement SignalR hubs for real-time dashboard functionality

  - [x] 9.1 Create DashboardHub for real-time communications
    - Implement connection management and group subscriptions
    - Add warehouse and variant-specific group joining
    - Create alert subscription management
    - _Requirements: 10.4, 9.5_

  - [x] 9.2 Implement real-time notification services
    - Create DashboardNotificationService for metric updates
    - Build alert notification system for operational events
    - Add stock level change broadcasting
    - _Requirements: 10.1, 10.2, 10.3, 10.5_

  - [x] 9.3 Create background services for real-time data processing


    - Implement metric calculation background service
    - Build alert detection and notification service
    - Add reservation expiry monitoring service
    - _Requirements: 9.4, 10.1, 10.2_

  - [ ]* 9.4 Write integration tests for SignalR functionality
    - Test hub connection and group management
    - Verify real-time message delivery
    - Test alert notification timing and accuracy
    - _Requirements: 10.4, 10.5_


- [x] 10. Implement authentication, authorization, and security






  - [x] 10.1 Replace JWT with ASP.NET Core Identity authentication system


    - Remove existing JWT authentication middleware
    - Set up ASP.NET Core Identity with PostgreSQL Entity Framework
    - Implement user registration and login endpoints
    - Configure password policies and user management
    - Add JWT token generation with Identity integration
    - _Requirements: 13.1, 13.4_


  - [x] 10.2 Create role-based authorization system with Identity

    - Define roles and permissions using Identity framework
    - Implement authorization policies for API endpoints
    - Add tenant-aware access control with Identity claims
    - _Requirements: 13.2, 13.3, 13.5_

  - [ ]* 10.3 Write security tests
    - Test authentication and authorization flows
    - Verify tenant data isolation
    - Test Identity integration and token security
    - _Requirements: 13.1, 13.2, 13.5_



- [x] 11. Create audit trail and compliance features





  - [x] 11.1 Implement audit logging system


    - Create audit trail for all stock movements and changes
    - Add user action logging with timestamps and context
    - Implement immutable audit record storage
    - _Requirements: 11.1, 11.4, 11.5_

  - [x] 11.2 Build audit query and reporting capabilities


    - Create audit trail query endpoints with filtering
    - Implement paginated audit history retrieval
    - Add compliance reporting features
    - _Requirements: 11.2, 11.3_





- [x] 12. Implement performance optimizations and caching




  - [x] 12.1 Add read model materialization for dashboard queries

    - Create materialized views for dashboard metrics
    - Implement background refresh of aggregated data
    - Add caching layer for frequently accessed data
    - _Requirements: 4.5, 9.5_


  - [x] 12.2 Optimize database queries and indexing

    - Add database indexes for performance-critical queries
    - Implement query optimization for bulk operations
    - Configure query performance monitoring
    - _Requirements: 4.5, 4.1_


- [x] 13. Final integration and end-to-end testing








  - [x] 13.1 Create comprehensive API integration tests


    - Test complete stock movement workflows
    - Verify real-time dashboard update flows
    - Test multi-tenant data isolation
    - _Requirements: 5.1, 9.1, 13.5_

  - [x] 13.2 Implement end-to-end scenario testing


    - Test complete inventory management workflows
    - Verify reservation and refund processes
    - Test dashboard real-time updates and alerts
    - _Requirements: 6.1, 8.1, 10.1_

  - [ ]* 13.3 Performance and load testing
    - Test API response times under load
    - Verify SignalR performance with multiple connections
    - Test database performance with large datasets
    - _Requirements: 4.5, 10.4_