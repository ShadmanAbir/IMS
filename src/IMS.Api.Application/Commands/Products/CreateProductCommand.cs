using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Domain.Enums;

namespace IMS.Api.Application.Commands.Products;

/// <summary>
/// Command to create a new product
/// </summary>
public class CreateProductCommand : ICommand<Result<ProductDto>>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public List<CreateProductAttributeDto> Attributes { get; set; } = new();
}

/// <summary>
/// Validator for CreateProductCommand
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Product name is required and must not exceed 255 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(1000)
            .WithMessage("Product description is required and must not exceed 1000 characters");

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .When(x => x.CategoryId.HasValue)
            .WithMessage("CategoryId must be a valid GUID when provided");

        RuleForEach(x => x.Attributes)
            .SetValidator(new CreateProductAttributeDtoValidator());
    }
}

/// <summary>
/// Validator for CreateProductAttributeDto
/// </summary>
public class CreateProductAttributeDtoValidator : AbstractValidator<CreateProductAttributeDto>
{
    public CreateProductAttributeDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Attribute name is required and must not exceed 100 characters");

        RuleFor(x => x.Value)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Attribute value is required and must not exceed 500 characters");

        RuleFor(x => x.DataType)
            .NotEmpty()
            .Must(BeValidDataType)
            .WithMessage("DataType must be one of: String, Number, Boolean, Date");
    }

    private static bool BeValidDataType(string dataType)
    {
        return Enum.TryParse<AttributeDataType>(dataType, true, out _);
    }
}

/// <summary>
/// Handler for CreateProductCommand
/// </summary>
public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            CategoryId? categoryId = request.CategoryId.HasValue ? CategoryId.Create(request.CategoryId.Value) : null;

            // Create the product
            var product = Product.Create(request.Name, request.Description, tenantId, categoryId);

            // Add attributes if provided
            foreach (var attributeDto in request.Attributes)
            {
                if (Enum.TryParse<AttributeDataType>(attributeDto.DataType, true, out var dataType))
                {
                    product.AddAttribute(attributeDto.Name, attributeDto.Value, dataType);
                }
            }

            // Save the product
            await _productRepository.AddAsync(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Return the created product as DTO
            var productDto = new ProductDto
            {
                Id = product.Id.Value,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId?.Value,
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

            return Result<ProductDto>.Success(productDto);
        }
        catch (ArgumentException ex)
        {
            return Result<ProductDto>.Failure("INVALID_INPUT", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ProductDto>.Failure("BUSINESS_RULE_VIOLATION", ex.Message);
        }
    }
}