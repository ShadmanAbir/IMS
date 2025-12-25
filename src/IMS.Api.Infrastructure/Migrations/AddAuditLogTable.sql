-- Migration to add AuditLogs table
-- This script should be run manually against the PostgreSQL database

CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id" uuid NOT NULL,
    "Action" varchar(50) NOT NULL,
    "EntityType" varchar(100) NOT NULL,
    "EntityId" varchar(100),
    "ActorId" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "TimestampUtc" timestamp with time zone NOT NULL,
    "Description" varchar(1000) NOT NULL,
    "OldValues" jsonb,
    "NewValues" jsonb,
    "Context_IpAddress" varchar(45),
    "Context_UserAgent" varchar(500),
    "Context_CorrelationId" varchar(100),
    "Context_AdditionalData" jsonb,
    "WarehouseId" uuid,
    "VariantId" uuid,
    "Reason" varchar(500),
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_TimestampUtc" ON "AuditLogs" ("TimestampUtc");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_TenantId_TimestampUtc" ON "AuditLogs" ("TenantId", "TimestampUtc");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_EntityType_EntityId" ON "AuditLogs" ("EntityType", "EntityId");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_ActorId" ON "AuditLogs" ("ActorId");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Action" ON "AuditLogs" ("Action");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_WarehouseId" ON "AuditLogs" ("WarehouseId") WHERE "WarehouseId" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_VariantId" ON "AuditLogs" ("VariantId") WHERE "VariantId" IS NOT NULL;

-- Composite indexes for common query patterns
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_TenantId_Action_TimestampUtc" ON "AuditLogs" ("TenantId", "Action", "TimestampUtc");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_TenantId_EntityType_TimestampUtc" ON "AuditLogs" ("TenantId", "EntityType", "TimestampUtc");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_TenantId_ActorId_TimestampUtc" ON "AuditLogs" ("TenantId", "ActorId", "TimestampUtc");