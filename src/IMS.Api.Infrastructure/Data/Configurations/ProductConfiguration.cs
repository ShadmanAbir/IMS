using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Product aggregate root
/// Configures constructor-based initialization without parameterless constructor
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        // Configure ProductId value object
        builder.Property(p => p.Id)
            .HasConversion(
                id => id.Value,
                value => ProductId.Create(value))
            .IsRequired();

        builder.Property(p => p.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        // Configure CategoryId value object
        builder.Property(p => p.CategoryId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? CategoryId.Create(value.Value) : null);

        // Configure TenantId value object
        builder.Property(p => p.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value))
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .IsRequired();

        // Configure soft delete properties
        builder.Property(p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.DeletedAtUtc);

        builder.Property(p => p.DeletedBy)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.Create(value.Value) : null);

        // Configure owned entities for attributes
        builder.OwnsMany(p => p.Attributes, a =>
        {
            a.ToTable("ProductAttributes");
            a.WithOwner().HasForeignKey("ProductId");
            a.HasKey("Id");
            
            a.Property(attr => attr.Name)
                .HasMaxLength(100)
                .IsRequired();
            
            a.Property(attr => attr.Value)
                .HasMaxLength(500)
                .IsRequired();
            
            a.Property(attr => attr.DataType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            // Configure soft delete for attributes
            a.Property(attr => attr.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            a.Property(attr => attr.DeletedAtUtc);

            a.Property(attr => attr.DeletedBy)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? UserId.Create(value.Value) : null);
        });

        // Configure one-to-many relationship with Variants
        builder.HasMany(p => p.Variants)
            .WithOne()
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add indexes for performance
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => new { p.TenantId, p.CategoryId });
        builder.HasIndex(p => new { p.TenantId, p.Name });
        builder.HasIndex(p => p.IsDeleted);
    }
}