using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for InventoryItem aggregate root
/// Configures constructor-based initialization without parameterless constructor
/// </summary>
public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");

        builder.HasKey(i => i.Id);

        // Configure InventoryItemId value object
        builder.Property(i => i.Id)
            .HasConversion(
                id => id.Value,
                value => InventoryItemId.Create(value))
            .IsRequired();

        // Configure VariantId value object
        builder.Property(i => i.VariantId)
            .HasConversion(
                id => id.Value,
                value => VariantId.Create(value))
            .IsRequired();

        // Configure WarehouseId value object
        builder.Property(i => i.WarehouseId)
            .HasConversion(
                id => id.Value,
                value => WarehouseId.Create(value))
            .IsRequired();

        // Configure decimal precision for stock quantities
        builder.Property(i => i.TotalStock)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(i => i.ReservedStock)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(i => i.AllowNegativeStock)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.ExpiryDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(i => i.UpdatedAtUtc)
            .IsRequired();

        // Configure soft delete properties
        builder.Property(i => i.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.DeletedAtUtc);

        builder.Property(i => i.DeletedBy)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.Create(value.Value) : null);

        // Configure one-to-many relationship with StockMovements
        builder.HasMany(i => i.Movements)
            .WithOne()
            .HasForeignKey("InventoryItemId")
            .OnDelete(DeleteBehavior.Cascade);

        // Add unique constraint for VariantId + WarehouseId combination
        builder.HasIndex(i => new { i.VariantId, i.WarehouseId })
            .IsUnique();

        // Add indexes for performance optimization
        builder.HasIndex(i => i.VariantId);
        builder.HasIndex(i => i.WarehouseId);
        builder.HasIndex(i => i.TotalStock);
        builder.HasIndex(i => i.UpdatedAtUtc);
        builder.HasIndex(i => i.IsDeleted);
        builder.HasIndex(i => i.ExpiryDate);
        builder.HasIndex(i => new { i.ExpiryDate, i.TotalStock }); // For expiry alerts with stock

        // Configure relationship with Variant
        builder.HasOne<Variant>()
            .WithMany()
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}