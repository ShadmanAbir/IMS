using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Commands.Variants;

/// <summary>
/// Command to soft delete a variant
/// </summary>
public class DeleteVariantCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public Guid DeletedBy { get; set; }
}

/// <summary>
/// Validator for DeleteVariantCommand
/// </summary>
public class DeleteVariantCommandValidator : AbstractValidator<DeleteVariantCommand>
{
    public DeleteVariantCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Variant ID is required");

        RuleFor(x => x.DeletedBy)
            .NotEmpty()
            .WithMessage("DeletedBy user ID is required");
    }
}

/// <summary>
/// Handler for DeleteVariantCommand
/// </summary>
public class DeleteVariantCommandHandler : ICommandHandler<DeleteVariantCommand, Result>
{
    private readonly IVariantRepository _variantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteVariantCommandHandler(
        IVariantRepository variantRepository,
        IUnitOfWork unitOfWork)
    {
        _variantRepository = variantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteVariantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var variantId = VariantId.Create(request.Id);
            var variant = await _variantRepository.GetByIdAsync(variantId);

            if (variant == null)
            {
                return Result.Failure("VARIANT_NOT_FOUND", "Variant not found");
            }

            if (variant.IsDeleted)
            {
                return Result.Failure("VARIANT_ALREADY_DELETED", "Variant is already deleted");
            }

            var deletedBy = UserId.Create(request.DeletedBy);
            variant.SoftDelete(deletedBy);

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