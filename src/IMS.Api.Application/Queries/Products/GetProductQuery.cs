using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Queries.Products;

/// <summary>
/// Query to get a single product by ID
/// </summary>
public class GetProductQuery : IQuery<Result<ProductDto>>
{
    public Guid Id { get; set; }
    public bool IncludeVariants { get; set; } = false;
}

/// <summary>
/// Validator for GetProductQuery
/// </summary>
public class GetProductQueryValidator : AbstractValidator<GetProductQuery>
{
    public GetProductQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Product ID is required");
    }
}

/// <summary>
/// Handler for GetProductQuery
/// </summary>
public class GetProductQueryHandler : IQueryHandler<GetProductQuery, Result<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public GetProductQueryHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<ProductDto>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var productId = ProductId.Create(request.Id);
        var product = await _productRepository.GetByIdAsync(productId);

        if (product == null)
        {
            return Result<ProductDto>.Failure("PRODUCT_NOT_FOUND", "Product not found");
        }

        // Get category name if category is assigned
        string? categoryName = null;
        if (product.CategoryId != null)
        {
            var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
            categoryName = category?.Name;
        }

        var productDto = new ProductDto
        {
            Id = product.Id.Value,
            Name = product.Name,
            Description = product.Description,
            CategoryId = product.CategoryId?.Value,
            CategoryName = categoryName,
            TenantId = product.TenantId.Value,
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc,
            IsDeleted = product.IsDeleted,
            DeletedAtUtc = product.DeletedAtUtc,
            DeletedBy = product.DeletedBy?.Value,
            Attributes = product.Attributes.Select(a => new ProductAttributeDto
            {
                Id = a.Id,
                Name = a.Name,
                Value = a.Value,
                DataType = a.DataType.ToString(),
                CreatedAtUtc = a.CreatedAtUtc,
                UpdatedAtUtc = a.UpdatedAtUtc
            }).ToList(),
            VariantCount = product.Variants.Count
        };

        // Include variants if requested
        if (request.IncludeVariants)
        {
            productDto.Variants = product.Variants.Select(v => new VariantDto
            {
                Id = v.Id.Value,
                Sku = v.Sku.Value,
                Name = v.Name,
                BaseUnit = v.BaseUnit.Name,
                ProductId = v.ProductId.Value,
                ProductName = product.Name,
                CreatedAtUtc = v.CreatedAtUtc,
                UpdatedAtUtc = v.UpdatedAtUtc,
                IsDeleted = v.IsDeleted,
                DeletedAtUtc = v.DeletedAtUtc,
                DeletedBy = v.DeletedBy?.Value,
                Attributes = v.Attributes.Select(a => new VariantAttributeDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Value = a.Value,
                    DataType = a.DataType.ToString(),
                    CreatedAtUtc = a.CreatedAtUtc,
                    UpdatedAtUtc = a.UpdatedAtUtc
                }).ToList(),
                UnitConversions = v.UnitConversions.Select(c => new UnitConversionDto
                {
                    Id = c.Id,
                    FromUnit = c.FromUnit.Name,
                    ToUnit = c.ToUnit.Name,
                    ConversionFactor = c.ConversionFactor,
                    CreatedAtUtc = c.CreatedAtUtc
                }).ToList()
            }).ToList();
        }

        return Result<ProductDto>.Success(productDto);
    }
}