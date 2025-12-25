using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Queries.Variants;

/// <summary>
/// Query to get a single variant by ID
/// </summary>
public class GetVariantQuery : IQuery<Result<VariantDto>>
{
    public Guid Id { get; set; }
}

/// <summary>
/// Validator for GetVariantQuery
/// </summary>
public class GetVariantQueryValidator : AbstractValidator<GetVariantQuery>
{
    public GetVariantQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Variant ID is required");
    }
}

/// <summary>
/// Handler for GetVariantQuery
/// </summary>
public class GetVariantQueryHandler : IQueryHandler<GetVariantQuery, Result<VariantDto>>
{
    private readonly IVariantRepository _variantRepository;
    private readonly IProductRepository _productRepository;

    public GetVariantQueryHandler(
        IVariantRepository variantRepository,
        IProductRepository productRepository)
    {
        _variantRepository = variantRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<VariantDto>> Handle(GetVariantQuery request, CancellationToken cancellationToken)
    {
        var variantId = VariantId.Create(request.Id);
        var variant = await _variantRepository.GetByIdAsync(variantId);

        if (variant == null)
        {
            return Result<VariantDto>.Failure("VARIANT_NOT_FOUND", "Variant not found");
        }

        // Get product name
        var product = await _productRepository.GetByIdAsync(variant.ProductId);
        var productName = product?.Name;

        var variantDto = new VariantDto
        {
            Id = variant.Id.Value,
            Sku = variant.Sku.Value,
            Name = variant.Name,
            BaseUnit = variant.BaseUnit.Name,
            ProductId = variant.ProductId.Value,
            ProductName = productName,
            CreatedAtUtc = variant.CreatedAtUtc,
            UpdatedAtUtc = variant.UpdatedAtUtc,
            IsDeleted = variant.IsDeleted,
            DeletedAtUtc = variant.DeletedAtUtc,
            DeletedBy = variant.DeletedBy?.Value,
            Attributes = variant.Attributes.Select(a => new VariantAttributeDto
            {
                Id = a.Id,
                Name = a.Name,
                Value = a.Value,
                DataType = a.DataType.ToString(),
                CreatedAtUtc = a.CreatedAtUtc,
                UpdatedAtUtc = a.UpdatedAtUtc
            }).ToList(),
            UnitConversions = variant.UnitConversions.Select(c => new UnitConversionDto
            {
                Id = c.Id,
                FromUnit = c.FromUnit.Name,
                ToUnit = c.ToUnit.Name,
                ConversionFactor = c.ConversionFactor,
                CreatedAtUtc = c.CreatedAtUtc
            }).ToList()
        };

        return Result<VariantDto>.Success(variantDto);
    }
}