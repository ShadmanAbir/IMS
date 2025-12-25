using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Domain.Aggregates;

namespace IMS.Api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Alert entity
/// </summary>
public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");

        builder.HasKey(a => a.Id);

        // Configure AlertId value object
        builder.Property(a => a.Id)
            .HasConversion(
                id => id.Value,
                value => AlertId.Create(value))
            .IsRequired();

        // Configure AlertType enum
        builder.Property(a => a.AlertType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Configure AlertSeverity enum
        builder.Property(a => a.Severity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Title)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.Message)
            .HasMaxLength(1000)
            .IsRequired();

        // Configure VariantId value object (nullable)
        builder.Property(a => a.VariantId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? VariantId.Create(value.Value) : null);

        // Configure WarehouseId value object (nullable)
        builder.Property(a => a.WarehouseId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? WarehouseId.Create(value.Value) : null);

        // Configure TenantId value object
        builder.Property(a => a.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value))
            .IsRequired();

        builder.Property(a => a.Data)
            .HasColumnType("jsonb") // PostgreSQL JSON column
            .HasDefaultValue("{}");

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        builder.Property(a => a.AcknowledgedAtUtc);

        // Configure AcknowledgedBy value object (nullable)
        builder.Property(a => a.AcknowledgedBy)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.Create(value.Value) : null);

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Configure soft delete properties
        builder.Property(a => a.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.DeletedAtUtc);

        builder.Property(a => a.DeletedBy)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.Create(value.Value) : null);

        // Add indexes for performance optimization
        builder.HasIndex(a => a.AlertType);
        builder.HasIndex(a => a.Severity);
        builder.HasIndex(a => a.VariantId);
        builder.HasIndex(a => a.WarehouseId);
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.CreatedAtUtc);
        builder.HasIndex(a => a.IsActive);
        builder.HasIndex(a => a.IsDeleted);
        builder.HasIndex(a => a.AcknowledgedAtUtc);
        builder.HasIndex(a => new { a.IsActive, a.IsDeleted, a.CreatedAtUtc }); // For active alerts query
        builder.HasIndex(a => new { a.AlertType, a.Severity, a.IsActive }); // For filtered alerts

        // Configure relationships
        builder.HasOne<Variant>()
            .WithMany()
            .HasForeignKey(a => a.VariantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(a => a.WarehouseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}