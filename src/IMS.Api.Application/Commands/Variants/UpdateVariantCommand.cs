using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Commands.Variants;

/// <summary>
/// Command to update an existing variant
/// </summary>
public class UpdateVariantCommand : ICommand<Result<VariantDto>>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Validator for UpdateVariantCommand
/// </summary>
public class UpdateVariantCommandValidator : AbstractValidator<UpdateVariantCommand>
{
    public UpdateVariantCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Variant ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Variant name is required and must not exceed 255 characters");
    }
}

/// <summary>
/// Handler for UpdateVariantCommand
/// </summary>
public class UpdateVariantCommandHandler : ICommandHandler<UpdateVariantCommand, Result<VariantDto>>
{
    private readonly IVariantRepository _variantRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateVariantCommandHandler(
        IVariantRepository variantRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _variantRepository = variantRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<VariantDto>> Handle(UpdateVariantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var variantId = VariantId.Create(request.Id);
            var variant = await _variantRepository.GetByIdAsync(variantId);

            if (variant == null)
            {
                return Result<VariantDto>.Failure("VARIANT_NOT_FOUND", "Variant not found");
            }

            // Update variant name
            variant.UpdateName(request.Name);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Get product name for DTO
            var product = await _productRepository.GetByIdAsync(variant.ProductId);
            var productName = product?.Name;

            // Return the updated variant as DTO
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