using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Infrastructure.Data.Configurations;

public class VariantConfiguration : IEntityTypeConfiguration<Variant>
{
    public void Configure(EntityTypeBuilder<Variant> builder)
    {
        builder.ToTable("Variants");

        builder.HasKey(v => v.Id);

        // Configure VariantId value object
        builder.Property(v => v.Id)
            .HasConversion(
                id => id.Value,
                value => VariantId.Create(value))
            .IsRequired();

        // Configure SKU value object
        builder.Property(v => v.Sku)
            .HasConversion(
                sku => sku.Value,
                value => SKU.Create(value))
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.Name)
            .HasMaxLength(255)
            .IsRequired();

        // Configure UnitOfMeasure value object
        builder.OwnsOne(v => v.BaseUnit, unit =>
        {
            unit.Property(u => u.Name)
                .HasColumnName("BaseUnitName")
                .HasMaxLength(50)
                .IsRequired();
            
            unit.Property(u => u.Symbol)
                .HasColumnName("BaseUnitSymbol")
                .HasMaxLength(10)
                .IsRequired();
            
            unit.Property(u => u.Type)
                .HasColumnName("BaseUnitType")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
        });

        // Configure ProductId value object
        builder.Property(v => v.ProductId)
            .HasConversion(
                id => id.Value,
                value => ProductId.Create(value))
            .IsRequired();

        builder.Property(v => v.CreatedAtUtc)
            .IsRequired();

        builder.Property(v => v.UpdatedAtUtc)
            .IsRequired();

        // Configure owned entities for attributes
        builder.OwnsMany(v => v.Attributes, a =>
        {
            a.ToTable("VariantAttributes");
            a.WithOwner().HasForeignKey("VariantId");
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
        });

        // Configure owned entities for unit conversions
        builder.OwnsMany(v => v.UnitConversions, uc =>
        {
            uc.ToTable("UnitConversions");
            uc.WithOwner().HasForeignKey("VariantId");
            uc.HasKey("Id");
            
            // Configure FromUnit
            uc.OwnsOne(u => u.FromUnit, from =>
            {
                from.Property(f => f.Name)
                    .HasColumnName("FromUnitName")
                    .HasMaxLength(50)
                    .IsRequired();
                
                from.Property(f => f.Symbol)
                    .HasColumnName("FromUnitSymbol")
                    .HasMaxLength(10)
                    .IsRequired();
                
                from.Property(f => f.Type)
                    .HasColumnName("FromUnitType")
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();
            });
            
            // Configure ToUnit
            uc.OwnsOne(u => u.ToUnit, to =>
            {
                to.Property(t => t.Name)
                    .HasColumnName("ToUnitName")
                    .HasMaxLength(50)
                    .IsRequired();
                
                to.Property(t => t.Symbol)
                    .HasColumnName("ToUnitSymbol")
                    .HasMaxLength(10)
                    .IsRequired();
                
                to.Property(t => t.Type)
                    .HasColumnName("ToUnitType")
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();
            });
            
            uc.Property(u => u.ConversionFactor)
                .HasPrecision(18, 6)
                .IsRequired();
        });

        // Add unique constraint for SKU
        builder.HasIndex(v => v.Sku)
            .IsUnique();

        // Add indexes for performance
        builder.HasIndex(v => v.ProductId);
        builder.HasIndex(v => v.Name);

        // Configure relationship with Product
        builder.HasOne<Product>()
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}