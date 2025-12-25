using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMS.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Migration to add performance-critical indexes for dashboard queries and bulk operations
    /// </summary>
    public partial class AddPerformanceIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Composite indexes for dashboard queries
            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Dashboard_Query",
                table: "InventoryItems",
                columns: new[] { "TenantId", "WarehouseId", "TotalStock", "ReservedStock", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_LowStock_Query",
                table: "InventoryItems",
                columns: new[] { "TenantId", "TotalStock", "IsDeleted" })
                .Annotation("Npgsql:IndexInclude", new[] { "VariantId", "WarehouseId", "ReservedStock" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Expiry_Query",
                table: "InventoryItems",
                columns: new[] { "TenantId", "ExpiryDate", "TotalStock", "IsDeleted" })
                .Annotation("Npgsql:IndexInclude", new[] { "VariantId", "WarehouseId" });

            // Stock movement performance indexes
            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_Dashboard_Rates",
                table: "StockMovements",
                columns: new[] { "TenantId", "Type", "TimestampUtc" })
                .Annotation("Npgsql:IndexInclude", new[] { "Quantity", "InventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_History_Query",
                table: "StockMovements",
                columns: new[] { "InventoryItemId", "TimestampUtc" })
                .Annotation("Npgsql:IndexInclude", new[] { "Type", "Quantity", "RunningBalance", "Reason", "ActorId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_Reference_Lookup",
                table: "StockMovements",
                columns: new[] { "TenantId", "ReferenceNumber" })
                .Annotation("Npgsql:IndexInclude", new[] { "Type", "Quantity", "TimestampUtc" });

            // Variant lookup optimizations
            migrationBuilder.CreateIndex(
                name: "IX_Variants_Bulk_Query",
                table: "Variants",
                columns: new[] { "TenantId", "ProductId", "IsDeleted" })
                .Annotation("Npgsql:IndexInclude", new[] { "Name", "BaseUnitName" });

            migrationBuilder.CreateIndex(
                name: "IX_Variants_SKU_Lookup",
                table: "Variants",
                columns: new[] { "TenantId", "Sku", "IsDeleted" });

            // Product search optimizations
            migrationBuilder.CreateIndex(
                name: "IX_Products_Category_Search",
                table: "Products",
                columns: new[] { "TenantId", "CategoryId", "IsDeleted" })
                .Annotation("Npgsql:IndexInclude", new[] { "Name", "Description" });

            // Warehouse performance indexes
            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Tenant_Active",
                table: "Warehouses",
                columns: new[] { "TenantId", "IsDeleted" })
                .Annotation("Npgsql:IndexInclude", new[] { "Name", "Location" });

            // Alert system indexes
            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Active_Query",
                table: "Alerts",
                columns: new[] { "TenantId", "IsActive", "AcknowledgedAtUtc", "CreatedAtUtc" })
                .Annotation("Npgsql:IndexInclude", new[] { "AlertType", "Severity", "VariantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Type_Severity",
                table: "Alerts",
                columns: new[] { "TenantId", "AlertType", "Severity", "IsActive" });

            // Reservation performance indexes
            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Expiry_Check",
                table: "Reservations",
                columns: new[] { "TenantId", "ExpiresAtUtc", "Status" })
                .Annotation("Npgsql:IndexInclude", new[] { "VariantId", "WarehouseId", "Quantity" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Variant_Warehouse",
                table: "Reservations",
                columns: new[] { "TenantId", "VariantId", "WarehouseId", "Status" })
                .Annotation("Npgsql:IndexInclude", new[] { "Quantity", "ExpiresAtUtc" });

            // Audit log performance indexes
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entity_Query",
                table: "AuditLogs",
                columns: new[] { "TenantId", "EntityType", "EntityId", "TimestampUtc" })
                .Annotation("Npgsql:IndexInclude", new[] { "Action", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_User_Activity",
                table: "AuditLogs",
                columns: new[] { "TenantId", "UserId", "TimestampUtc" })
                .Annotation("Npgsql:IndexInclude", new[] { "Action", "EntityType" });

            // Partial indexes for better performance on filtered queries
            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY IX_InventoryItems_Available_Stock 
                ON ""InventoryItems"" (""TenantId"", ""WarehouseId"", (""TotalStock"" - ""ReservedStock"")) 
                WHERE ""IsDeleted"" = false AND ""TotalStock"" > 0;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY IX_StockMovements_Recent 
                ON ""StockMovements"" (""InventoryItemId"", ""TimestampUtc"" DESC) 
                WHERE ""TimestampUtc"" >= NOW() - INTERVAL '30 days';
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY IX_Alerts_Unacknowledged 
                ON ""Alerts"" (""TenantId"", ""CreatedAtUtc"" DESC) 
                WHERE ""IsActive"" = true AND ""AcknowledgedAtUtc"" IS NULL AND ""IsDeleted"" = false;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop custom partial indexes
            migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS IX_InventoryItems_Available_Stock;");
            migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS IX_StockMovements_Recent;");
            migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS IX_Alerts_Unacknowledged;");

            // Drop regular indexes
            migrationBuilder.DropIndex(name: "IX_InventoryItems_Dashboard_Query", table: "InventoryItems");
            migrationBuilder.DropIndex(name: "IX_InventoryItems_LowStock_Query", table: "InventoryItems");
            migrationBuilder.DropIndex(name: "IX_InventoryItems_Expiry_Query", table: "InventoryItems");
            migrationBuilder.DropIndex(name: "IX_StockMovements_Dashboard_Rates", table: "StockMovements");
            migrationBuilder.DropIndex(name: "IX_StockMovements_History_Query", table: "StockMovements");
            migrationBuilder.DropIndex(name: "IX_StockMovements_Reference_Lookup", table: "StockMovements");
            migrationBuilder.DropIndex(name: "IX_Variants_Bulk_Query", table: "Variants");
            migrationBuilder.DropIndex(name: "IX_Variants_SKU_Lookup", table: "Variants");
            migrationBuilder.DropIndex(name: "IX_Products_Category_Search", table: "Products");
            migrationBuilder.DropIndex(name: "IX_Warehouses_Tenant_Active", table: "Warehouses");
            migrationBuilder.DropIndex(name: "IX_Alerts_Active_Query", table: "Alerts");
            migrationBuilder.DropIndex(name: "IX_Alerts_Type_Severity", table: "Alerts");
            migrationBuilder.DropIndex(name: "IX_Reservations_Expiry_Check", table: "Reservations");
            migrationBuilder.DropIndex(name: "IX_Reservations_Variant_Warehouse", table: "Reservations");
            migrationBuilder.DropIndex(name: "IX_AuditLogs_Entity_Query", table: "AuditLogs");
            migrationBuilder.DropIndex(name: "IX_AuditLogs_User_Activity", table: "AuditLogs");
        }
    }
}