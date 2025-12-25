using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for DashboardMetricsCache entity
/// </summary>
public class DashboardMetricsCacheConfiguration : IEntityTypeConfiguration<DashboardMetricsCache>
{
    public void Configure(EntityTypeBuilder<DashboardMetricsCache> builder)
    {
        builder.ToTable("DashboardMetricsCache");

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

        builder.Property(x => x.PeriodStart)
            .IsRequired();

        builder.Property(x => x.PeriodEnd)
            .IsRequired();

        builder.Property(x => x.PeriodType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.TotalStockValue)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.TotalAvailableStock)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.TotalReservedStock)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.LowStockVariantCount)
            .IsRequired();

        builder.Property(x => x.OutOfStockVariantCount)
            .IsRequired();

        builder.Property(x => x.ExpiredVariantCount)
            .IsRequired();

        builder.Property(x => x.ExpiringVariantCount)
            .IsRequired();

        builder.Property(x => x.TotalVariantCount)
            .IsRequired();

        builder.Property(x => x.TotalMovementVolume)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.TotalMovementCount)
            .IsRequired();

        builder.Property(x => x.AverageMovementSize)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.CalculatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.IsStale)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.MetadataJson)
            .HasColumnType("jsonb")
            .IsRequired(false);

        // Indexes for performance
        builder.HasIndex(x => new { x.TenantId, x.WarehouseId, x.PeriodStart, x.PeriodEnd, x.PeriodType })
            .HasDatabaseName("IX_DashboardMetricsCache_Lookup")
            .IsUnique();

        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("IX_DashboardMetricsCache_Expiry");

        builder.HasIndex(x => x.IsStale)
            .HasDatabaseName("IX_DashboardMetricsCache_Stale");

        builder.HasIndex(x => x.CalculatedAtUtc)
            .HasDatabaseName("IX_DashboardMetricsCache_Calculated");

        // Global query filter for tenant isolation
        builder.HasQueryFilter(x => !x.IsStale);
    }
}