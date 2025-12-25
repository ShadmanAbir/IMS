using IMS.Api.Domain.Entities;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMS.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for AuditLog entity
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        // Primary key
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(
                v => v.Value,
                v => AuditLogId.Create(v))
            .IsRequired();

        // Action
        builder.Property(a => a.Action)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Entity information
        builder.Property(a => a.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasMaxLength(100);

        // Actor and tenant
        builder.Property(a => a.ActorId)
            .HasConversion(
                v => v.Value,
                v => UserId.Create(v))
            .IsRequired();

        builder.Property(a => a.TenantId)
            .HasConversion(
                v => v.Value,
                v => TenantId.Create(v))
            .IsRequired();

        // Timestamp
        builder.Property(a => a.TimestampUtc)
            .IsRequired();

        // Description and reason
        builder.Property(a => a.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.Reason)
            .HasMaxLength(500);

        // JSON data
        builder.Property(a => a.OldValues)
            .HasColumnType("jsonb");

        builder.Property(a => a.NewValues)
            .HasColumnType("jsonb");

        // Context as owned entity
        builder.OwnsOne(a => a.Context, context =>
        {
            context.Property(c => c.IpAddress)
                .HasColumnName("Context_IpAddress")
                .HasMaxLength(45); // IPv6 max length

            context.Property(c => c.UserAgent)
                .HasColumnName("Context_UserAgent")
                .HasMaxLength(500);

            context.Property(c => c.CorrelationId)
                .HasColumnName("Context_CorrelationId")
                .HasMaxLength(100);

            context.Property(c => c.AdditionalData)
                .HasColumnName("Context_AdditionalData")
                .HasColumnType("jsonb");
        });

        // Optional warehouse and variant references
        builder.Property(a => a.WarehouseId)
            .HasConversion(
                v => v != null ? v.Value : (Guid?)null,
                v => v.HasValue ? WarehouseId.Create(v.Value) : null);

        builder.Property(a => a.VariantId)
            .HasConversion(
                v => v != null ? v.Value : (Guid?)null,
                v => v.HasValue ? VariantId.Create(v.Value) : null);

        // Indexes for performance
        builder.HasIndex(a => a.TimestampUtc)
            .HasDatabaseName("IX_AuditLogs_TimestampUtc");

        builder.HasIndex(a => new { a.TenantId, a.TimestampUtc })
            .HasDatabaseName("IX_AuditLogs_TenantId_TimestampUtc");

        builder.HasIndex(a => new { a.EntityType, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_EntityType_EntityId");

        builder.HasIndex(a => a.ActorId)
            .HasDatabaseName("IX_AuditLogs_ActorId");

        builder.HasIndex(a => a.Action)
            .HasDatabaseName("IX_AuditLogs_Action");

        builder.HasIndex(a => a.WarehouseId)
            .HasDatabaseName("IX_AuditLogs_WarehouseId")
            .HasFilter("WarehouseId IS NOT NULL");

        builder.HasIndex(a => a.VariantId)
            .HasDatabaseName("IX_AuditLogs_VariantId")
            .HasFilter("VariantId IS NOT NULL");

        // Composite indexes for common query patterns
        builder.HasIndex(a => new { a.TenantId, a.Action, a.TimestampUtc })
            .HasDatabaseName("IX_AuditLogs_TenantId_Action_TimestampUtc");

        builder.HasIndex(a => new { a.TenantId, a.EntityType, a.TimestampUtc })
            .HasDatabaseName("IX_AuditLogs_TenantId_EntityType_TimestampUtc");

        builder.HasIndex(a => new { a.TenantId, a.ActorId, a.TimestampUtc })
            .HasDatabaseName("IX_AuditLogs_TenantId_ActorId_TimestampUtc");
    }
}