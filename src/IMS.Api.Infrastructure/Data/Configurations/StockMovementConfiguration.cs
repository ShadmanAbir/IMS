using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Domain.Enums;

namespace IMS.Api.Infrastructure.Data.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(sm => sm.Id);

        // Configure StockMovementId value object
        builder.Property(sm => sm.Id)
            .HasConversion(
                id => id.Value,
                value => StockMovementId.Create(value))
            .IsRequired();

        // Configure MovementType enum
        builder.Property(sm => sm.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Configure decimal precision for quantities
        builder.Property(sm => sm.Quantity)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(sm => sm.RunningBalance)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(sm => sm.Reason)
            .HasMaxLength(255)
            .IsRequired();

        // Configure UserId value object
        builder.Property(sm => sm.ActorId)
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value))
            .IsRequired();

        builder.Property(sm => sm.TimestampUtc)
            .IsRequired();

        builder.Property(sm => sm.ReferenceNumber)
            .HasMaxLength(100);

        // Configure MovementMetadata value object as JSON
        builder.OwnsOne(sm => sm.Metadata, metadata =>
        {
            metadata.Property(m => m.Data)
                .HasColumnName("Metadata")
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    data => System.Text.Json.JsonSerializer.Serialize(data, (System.Text.Json.JsonSerializerOptions)null),
                    json => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json, (System.Text.Json.JsonSerializerOptions)null) ?? new Dictionary<string, object>());
        });

        // Add indexes for performance optimization on frequently queried columns
        builder.HasIndex(sm => sm.Type);
        builder.HasIndex(sm => sm.TimestampUtc);
        builder.HasIndex(sm => sm.ActorId);
        builder.HasIndex(sm => sm.ReferenceNumber);
        builder.HasIndex("InventoryItemId"); // Foreign key index

        // Configure relationship with InventoryItem
        builder.HasOne<InventoryItem>()
            .WithMany(i => i.Movements)
            .HasForeignKey("InventoryItemId")
            .OnDelete(DeleteBehavior.Cascade);

        // Add shadow property for InventoryItemId foreign key with value conversion
        builder.Property<InventoryItemId>("InventoryItemId")
            .HasConversion(
                id => id.Value,
                value => InventoryItemId.Create(value))
            .IsRequired();
    }
}