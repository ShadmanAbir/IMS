using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;
using IMS.Api.Domain.Enums;

namespace IMS.Api.Application.Commands.Variants;

/// <summary>
/// Command to create a new variant
/// </summary>
public class CreateVariantCommand : ICommand<Result<VariantDto>>
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public List<CreateVariantAttributeDto> Attributes { get; set; } = new();
    public List<CreateUnitConversionDto> UnitConversions { get; set; } = new();
}

/// <summary>
/// Validator for CreateVariantCommand
/// </summary>
public class CreateVariantCommandValidator : AbstractValidator<CreateVariantCommand>
{
    public CreateVariantCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("SKU is required and must not exceed 100 characters");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Variant name is required and must not exceed 255 characters");

        RuleFor(x => x.BaseUnit)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Base unit is required and must not exceed 50 characters");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required");

        RuleForEach(x => x.Attributes)
            .SetValidator(new CreateVariantAttributeDtoValidator());

        RuleForEach(x => x.UnitConversions)
            .SetValidator(new CreateUnitConversionDtoValidator());
    }
}

/// <summary>
/// Validator for CreateVariantAttributeDto
/// </summary>
public class CreateVariantAttributeDtoValidator : AbstractValidator<CreateVariantAttributeDto>
{
    public CreateVariantAttributeDtoValidator()
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
/// Validator for CreateUnitConversionDto
/// </summary>
public class CreateUnitConversionDtoValidator : AbstractValidator<CreateUnitConversionDto>
{
    public CreateUnitConversionDtoValidator()
    {
        RuleFor(x => x.FromUnit)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("From unit is required and must not exceed 50 characters");

        RuleFor(x => x.ToUnit)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("To unit is required and must not exceed 50 characters");

        RuleFor(x => x.ConversionFactor)
            .GreaterThan(0)
            .WithMessage("Conversion factor must be greater than 0");
    }
}

/// <summary>
/// Handler for CreateVariantCommand
/// </summary>
public class CreateVariantCommandHandler : ICommandHandler<CreateVariantCommand, Result<VariantDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IVariantRepository _variantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVariantCommandHandler(
        IProductRepository productRepository,
        IVariantRepository variantRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _variantRepository = variantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<VariantDto>> Handle(CreateVariantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var productId = ProductId.Create(request.ProductId);
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null)
            {
                return Result<VariantDto>.Failure("PRODUCT_NOT_FOUND", "Product not found");
            }

            // Check if SKU already exists
            var existingVariant = await _variantRepository.GetBySkuAsync(SKU.Create(request.Sku));
            if (existingVariant != null)
            {
                return Result<VariantDto>.Failure("DUPLICATE_SKU", "A variant with this SKU already exists");
            }

            // Create the variant
            var sku = SKU.Create(request.Sku);
            var baseUnit = UnitOfMeasure.Create(request.BaseUnit, request.BaseUnit, UnitType.Count); // Default to Count, should be determined by business logic
            var variant = product.AddVariant(sku, request.Name, baseUnit);

            // Add attributes if provided
            foreach (var attributeDto in request.Attributes)
            {
                if (Enum.TryParse<AttributeDataType>(attributeDto.DataType, true, out var dataType))
                {
                    variant.AddAttribute(attributeDto.Name, attributeDto.Value, dataType);
                }
            }

            // Add unit conversions if provided
            foreach (var conversionDto in request.UnitConversions)
            {
                var fromUnit = UnitOfMeasure.Create(conversionDto.FromUnit, conversionDto.FromUnit, UnitType.Count); // Should be determined by business logic
                var toUnit = UnitOfMeasure.Create(conversionDto.ToUnit, conversionDto.ToUnit, UnitType.Count);
                variant.AddUnitConversion(fromUnit, toUnit, conversionDto.ConversionFactor);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Return the created variant as DTO
            var variantDto = new VariantDto
            {
                Id = variant.Id.Value,
                Sku = variant.Sku.Value,
                Name = variant.Name,
                BaseUnit = variant.BaseUnit.Name,
                ProductId = variant.ProductId.Value,
                ProductName = product.Name,
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
        catch (ArgumentException ex)
        {
            return Result<VariantDto>.Failure("INVALID_INPUT", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result<VariantDto>.Failure("BUSINESS_RULE_VIOLATION", ex.Message);
        }
    }
}