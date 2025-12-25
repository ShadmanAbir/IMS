using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Commands.Products;

/// <summary>
/// Command to soft delete a product
/// </summary>
public class DeleteProductCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public Guid DeletedBy { get; set; }
}

/// <summary>
/// Validator for DeleteProductCommand
/// </summary>
public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Product ID is required");

        RuleFor(x => x.DeletedBy)
            .NotEmpty()
            .WithMessage("DeletedBy user ID is required");
    }
}

/// <summary>
/// Handler for DeleteProductCommand
/// </summary>
public class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, Result>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var productId = ProductId.Create(request.Id);
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null)
            {
                return Result.Failure("PRODUCT_NOT_FOUND", "Product not found");
            }

            if (product.IsDeleted)
            {
                return Result.Failure("PRODUCT_ALREADY_DELETED", "Product is already deleted");
            }

            var deletedBy = UserId.Create(request.DeletedBy);
            product.SoftDelete(deletedBy);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure("INVALID_INPUT", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure("BUSINESS_RULE_VIOLATION", ex.Message);
        }
    }
}