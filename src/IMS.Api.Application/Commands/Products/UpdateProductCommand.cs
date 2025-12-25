using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Commands.Products;

/// <summary>
/// Command to update an existing product
/// </summary>
public class UpdateProductCommand : ICommand<Result<ProductDto>>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
}

/// <summary>
/// Validator for UpdateProductCommand
/// </summary>
public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Product ID is required");

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
    }
}

/// <summary>
/// Handler for UpdateProductCommand
/// </summary>
public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var productId = ProductId.Create(request.Id);
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null)
            {
                return Result<ProductDto>.Failure("PRODUCT_NOT_FOUND", "Product not found");
            }

            // Update product details
            product.UpdateDetails(request.Name, request.Description);

            // Update category if provided
            CategoryId? categoryId = request.CategoryId.HasValue ? CategoryId.Create(request.CategoryId.Value) : null;
            product.UpdateCategory(categoryId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Return the updated product as DTO
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