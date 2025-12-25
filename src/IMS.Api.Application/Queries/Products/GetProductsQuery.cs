using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Queries.Products;

/// <summary>
/// Query to get a paginated list of products
/// </summary>
public class GetProductsQuery : IQuery<Result<PagedResult<ProductDto>>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IncludeDeleted { get; set; } = false;
    public bool IncludeVariants { get; set; } = false;
}

/// <summary>
/// Validator for GetProductsQuery
/// </summary>
public class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
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
    }
}

/// <summary>
/// Handler for GetProductsQuery
/// </summary>
public class GetProductsQueryHandler : IQueryHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public GetProductsQueryHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        CategoryId? categoryId = request.CategoryId.HasValue ? CategoryId.Create(request.CategoryId.Value) : null;

        var pagedProducts = await _productRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.SearchTerm,
            categoryId,
            request.IncludeDeleted);

        // Get category names for products that have categories
        var categoryIds = pagedProducts.Items
            .Where(p => p.CategoryId != null)
            .Select(p => p.CategoryId!)
            .Distinct()
            .ToList();

        var categories = new Dictionary<CategoryId, string>();
        if (categoryIds.Any())
        {
            var categoryEntities = await _categoryRepository.GetByIdsAsync(categoryIds);
            categories = categoryEntities.ToDictionary(c => c.Id, c => c.Name);
        }

        var productDtos = pagedProducts.Items.Select(product =>
        {
            var productDto = new ProductDto
            {
                Id = product.Id.Value,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId?.Value,
                CategoryName = product.CategoryId != null && categories.ContainsKey(product.CategoryId) 
                    ? categories[product.CategoryId] 
                    : null,
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

            return productDto;
        }).ToList();

        var result = new PagedResult<ProductDto>(
            productDtos,
            pagedProducts.TotalCount,
            pagedProducts.PageNumber,
            pagedProducts.PageSize);

        return Result<PagedResult<ProductDto>>.Success(result);
    }
}