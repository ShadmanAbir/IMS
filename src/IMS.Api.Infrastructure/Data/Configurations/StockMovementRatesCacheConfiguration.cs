using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for StockMovementRatesCache entity
/// </summary>
public class StockMovementRatesCacheConfiguration : IEntityTypeConfiguration<StockMovementRatesCache>
{
    public void Configure(EntityTypeBuilder<StockMovementRatesCache> builder)
    {
        builder.ToTable("StockMovementRatesCache");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .HasConversion(
                v => v.Value,
                v => TenantId.Create(v))
            .IsRequired();

        builder.Property(x => x.WarehouseId)
            .IsRequired(false);

        builder.Property(x => x.VariantId)
            .IsRequired(false);

        builder.Property(x => x.MovementType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PeriodStart)
            .IsRequired();

        builder.Property(x => x.PeriodEnd)
            .IsRequired();

        builder.Property(x => x.PeriodType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.TotalQuantity)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.MovementCount)
            .IsRequired();

        builder.Property(x => x.AverageQuantity)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.PercentageOfTotal)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(x => x.CalculatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.IsStale)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes for performance
        builder.HasIndex(x => new { x.TenantId, x.WarehouseId, x.VariantId, x.MovementType, x.PeriodStart, x.PeriodEnd, x.PeriodType })
            .HasDatabaseName("IX_StockMovementRatesCache_Lookup")
            .IsUnique();

        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("IX_StockMovementRatesCache_Expiry");

        builder.HasIndex(x => x.IsStale)
            .HasDatabaseName("IX_StockMovementRatesCache_Stale");

        builder.HasIndex(x => new { x.TenantId, x.MovementType, x.PeriodStart, x.PeriodEnd })
            .HasDatabaseName("IX_StockMovementRatesCache_MovementType");

        builder.HasIndex(x => new { x.TenantId, x.WarehouseId, x.PeriodStart, x.PeriodEnd })
            .HasDatabaseName("IX_StockMovementRatesCache_Warehouse");

        // Global query filter for tenant isolation
        builder.HasQueryFilter(x => !x.IsStale);
    }
}