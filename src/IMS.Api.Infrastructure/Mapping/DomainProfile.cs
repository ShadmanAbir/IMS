using AutoMapper;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Domain.Enums;
using IMS.Api.Infrastructure.Data.DTOs;
using IMS.Api.Application.Common.DTOs;
using System.Text.Json;

namespace IMS.Api.Infrastructure.Mapping;

/// <summary>
/// AutoMapper profile for mapping between domain entities and DTOs
/// Supports soft delete pattern and double-entry stock movements
/// Ensures proper value object conversions and maintains domain integrity
/// </summary>
public class DomainProfile : Profile
{
    public DomainProfile()
    {
        ConfigureProductMappings();
        ConfigureVariantMappings();
        ConfigureInventoryMappings();
        ConfigureStockMovementMappings();
        ConfigureWarehouseMappings();
        ConfigureCategoryMappings();
        ConfigureReservationMappings();
        ConfigurePricingMappings();
        ConfigureDashboardMappings();
        ConfigureAuthenticationMappings();
    }

    private void ConfigureProductMappings()
    {
        // Product mappings with soft delete support
        CreateMap<ProductDto, Product>()
            .ConstructUsing(dto => Product.Create(dto.Name, dto.Description, TenantId.Create(dto.TenantId), 
                dto.CategoryId.HasValue ? CategoryId.Create(dto.CategoryId.Value) : null))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => ProductId.Create(src.Id)))
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src => src.CreatedAtUtc))
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.MapFrom(src => src.UpdatedAtUtc));

        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId != null ? src.CategoryId.Value : (Guid?)null))
            .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId.Value))
            .ForMember(dest => dest.DeletedBy, opt => opt.MapFrom(src => src.DeletedBy != null ? src.DeletedBy.Value : (Guid?)null));

        // ProductAttribute mappings with soft delete support
        CreateMap<ProductAttributeDto, ProductAttribute>()
            .ConstructUsing(dto => ProductAttribute.Create(dto.Name, dto.Value, Enum.Parse<AttributeDataType>(dto.DataType)))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));

        CreateMap<ProductAttribute, ProductAttributeDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => src.DataType.ToString()))
            .ForMember(dest => dest.DeletedBy, opt => opt.MapFrom(src => src.DeletedBy != null ? src.DeletedBy.Value : (Guid?)null));
    }

    private void ConfigureVariantMappings()
    {
        // Variant mappings with soft delete support
        CreateMap<VariantDto, Variant>()
            .ConstructUsing(dto => Variant.Create(
                SKU.Create(dto.Sku), 
                dto.Name, 
                UnitOfMeasure.Create(dto.BaseUnitSymbol, dto.BaseUnitName, Enum.Parse<UnitType>(dto.BaseUnitType)),
                ProductId.Create(dto.ProductId)))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => VariantId.Create(src.Id)))
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src => src.CreatedAtUtc))
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.MapFrom(src => src.UpdatedAtUtc));

        CreateMap<Variant, VariantDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
            .ForMember(dest => dest.Sku, opt => opt.MapFrom(src => src.Sku.Value))
            .ForMember(dest => dest.BaseUnitSymbol, opt => opt.MapFrom(src => src.BaseUnit.Symbol))
            .ForMember(dest => dest.BaseUnitName, opt => opt.MapFrom(src => src.BaseUnit.Name))
            .ForMember(dest => dest.BaseUnitType, opt => opt.MapFrom(src => src.BaseUnit.Type.ToString()))
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId.Value))
            .ForMember(dest => dest.DeletedBy, opt => opt.MapFrom(src => src.DeletedBy != null ? src.DeletedBy.Value : (Guid?)null));

        // VariantAttribute mappings with soft delete support
        CreateMap<VariantAttributeDto, VariantAttribute>()
            .ConstructUsing(dto => VariantAttribute.Create(dto.Name, dto.Value, Enum.Parse<AttributeDataType>(dto.DataType)))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));

        CreateMap<VariantAttribute, VariantAttributeDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => src.DataType.ToString()))
            .ForMember(dest => dest.DeletedBy, opt => opt.MapFrom(src => src.DeletedBy != null ? src.DeletedBy.Value : (Guid?)null));
    }

    private void ConfigureInventoryMappings()
    {
        // InventoryItem mappings with soft delete and expiry support
        CreateMap<InventoryItemDto, InventoryItem>()
            .ConstructUsing(dto => InventoryItem.Create(
                VariantId.Create(dto.VariantId), 
                WarehouseId.Create(dto.WarehouseId), 
                dto.AllowNegativeStock,
                dto.ExpiryDate))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => InventoryItemId.Create(src.Id)))
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.MapFrom(src => src.UpdatedAtUtc));

        CreateMap<InventoryItem, InventoryItemDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
            .ForMember(dest => dest.VariantId, opt => opt.MapFrom(src => src.VariantId.Value))
            .ForMember(dest => dest.WarehouseId, opt => opt.MapFrom(src => src.WarehouseId.Value))
            .ForMember(dest => dest.DeletedBy, opt => opt.MapFrom(src => src.DeletedBy != null ? src.DeletedBy.Value : (Guid?)null));
    }

    private void ConfigureStockMovementMappings()
    {
        // StockMovement mappings with double-entry support
        CreateMap<StockMovementDto, StockMovement>()
            .ConstructUsing(dto => StockMovement.Create(
                Enum.Parse<MovementType>(dto.Type),
                dto.Quantity,
                dto.RunningBalance,
                dto.Reason,
                UserId.Create(dto.ActorId),
                dto.ReferenceNumber,
                string.IsNullOrEmpty(dto.Metadata) ? MovementMetadata.Empty() : 
                    MovementMetadata.Create(JsonSerializer.Deserialize<Dictionary<string, object>>(dto.Metadata) ?? new())))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => StockMovementId.Create(src.Id)))
            .ForMember(dest => dest.TimestampUtc, opt => opt.MapFrom(src => src.TimestampUtc))
            .ForMember(dest => dest.EntryType, opt => opt.MapFrom(src => Enum.Parse<DoubleEntryType>(src.EntryType)))
            .ForMember(dest => dest.PairedMovementId, opt => opt.MapFrom(src => 
                src.PairedMovementId.HasValue ? StockMovementId.Create(src.PairedMovementId.Value) : null));

        CreateMap<StockMovement, StockMovementDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.ActorId, opt => opt.MapFrom(src => src.ActorId.Value))
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => JsonSerializer.Serialize(src.Metadata.Data)))
            .ForMember(dest => dest.EntryType, opt => opt.MapFrom(src => src.EntryType.ToString()))
            .ForMember(dest => dest.PairedMovementId, opt => opt.MapFrom(src => 
                src.PairedMovementId != null ? src.PairedMovementId.Value : (Guid?)null));
    }

    private void ConfigureWarehouseMappings()
    {
        // Placeholder for warehouse mappings - will be implemented when Warehouse domain entity is created
        // CreateMap<WarehouseDto, Warehouse>()...
        // CreateMap<Warehouse, WarehouseDto>()...
    }

    private void ConfigureCategoryMappings()
    {
        // Placeholder for category mappings - will be implemented when Category domain entity is created
        // CreateMap<CategoryDto, Category>()...
        // CreateMap<Category, CategoryDto>()...
    }

    private void ConfigureReservationMappings()
    {
        // Placeholder for reservation mappings - will be implemented when Reservation domain entity is created
        // CreateMap<ReservationDto, Reservation>()...
        // CreateMap<Reservation, ReservationDto>()...
    }

    private void ConfigurePricingMappings()
    {
        // Placeholder for pricing mappings - will be implemented when Pricing domain entity is created
        // CreateMap<PricingDto, Pricing>()...
        // CreateMap<Pricing, PricingDto>()...
    }

    private void ConfigureDashboardMappings()
    {
        // Dashboard DTOs are typically used for read-only scenarios and don't need reverse mapping
        // These mappings will be handled by Dapper queries directly to DTOs for performance
    }

    private void ConfigureAuthenticationMappings()
    {
        // ApplicationUser to UserDto mapping
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId.Value))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src => src.CreatedAtUtc))
            .ForMember(dest => dest.LastLoginUtc, opt => opt.MapFrom(src => src.LastLoginUtc))
            .ForMember(dest => dest.Roles, opt => opt.Ignore()) // Populated separately
            .ForMember(dest => dest.Claims, opt => opt.Ignore()); // Populated separately

        // ApplicationRole mappings
        CreateMap<ApplicationRole, CreateRoleRequest>().ReverseMap();
    }
}