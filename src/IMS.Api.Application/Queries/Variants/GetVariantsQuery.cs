using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Queries.Variants;

/// <summary>
/// Query to get a paginated list of variants
/// </summary>
public class GetVariantsQuery : IQuery<Result<PagedResult<VariantDto>>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public Guid? ProductId { get; set; }
    public string? Sku { get; set; }
    public bool IncludeDeleted { get; set; } = false;
}

/// <summary>
/// Validator for GetVariantsQuery
/// </summary>
public class GetVariantsQueryValidator : AbstractValidator<GetVariantsQuery>
{
    public GetVariantsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(255)
            .When(x => !string.IsNullOrEmpty(x.SearchTerm))
            .WithMessage("Search term must not exceed 255 characters");

        RuleFor(x => x.Sku)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Sku))
            .WithMessage("SKU must not exceed 100 characters");
    }
}

/// <summary>
/// Handler for GetVariantsQuery
/// </summary>
public class GetVariantsQueryHandler : IQueryHandler<GetVariantsQuery, Result<PagedResult<VariantDto>>>
{
    private readonly IVariantRepository _variantRepository;
    private readonly IProductRepository _productRepository;

    public GetVariantsQueryHandler(
        IVariantRepository variantRepository,
        IProductRepository productRepository)
    {
        _variantRepository = variantRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<PagedResult<VariantDto>>> Handle(GetVariantsQuery request, CancellationToken cancellationToken)
    {
        ProductId? productId = request.ProductId.HasValue ? ProductId.Create(request.ProductId.Value) : null;

        var pagedVariants = await _variantRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.SearchTerm,
            productId,
            request.Sku,
            request.IncludeDeleted);

        // Get product names for variants
        var productIds = pagedVariants.Items
            .Select(v => v.ProductId)
            .Distinct()
            .ToList();

        var products = new Dictionary<ProductId, string>();
        if (productIds.Any())
        {
            var productEntities = await _productRepository.GetByIdsAsync(productIds);
            products = productEntities.ToDictionary(p => p.Id, p => p.Name);
        }

        var variantDtos = pagedVariants.Items.Select(variant => new VariantDto
        {
            Id = variant.Id.Value,
            Sku = variant.Sku.Value,
            Name = variant.Name,
            BaseUnit = variant.BaseUnit.Name,
            ProductId = variant.ProductId.Value,
            ProductName = products.ContainsKey(variant.ProductId) ? products[variant.ProductId] : null,
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
        }).ToList();

        var result = new PagedResult<VariantDto>(
            variantDtos,
            pagedVariants.TotalCount,
            pagedVariants.PageNumber,
            pagedVariants.PageSize);

        return Result<PagedResult<VariantDto>>.Success(result);
    }
}