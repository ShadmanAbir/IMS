using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Commands.Warehouses;

/// <summary>
/// Command to soft delete a warehouse
/// </summary>
public class DeleteWarehouseCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public Guid DeletedBy { get; set; }
}

/// <summary>
/// Validator for DeleteWarehouseCommand
/// </summary>
public class DeleteWarehouseCommandValidator : AbstractValidator<DeleteWarehouseCommand>
{
    public DeleteWarehouseCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Warehouse ID is required");

        RuleFor(x => x.DeletedBy)
            .NotEmpty()
            .WithMessage("DeletedBy user ID is required");
    }
}

/// <summary>
/// Handler for DeleteWarehouseCommand
/// </summary>
public class DeleteWarehouseCommandHandler : ICommandHandler<DeleteWarehouseCommand, Result>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWarehouseCommandHandler(
        IWarehouseRepository warehouseRepository,
        IUnitOfWork unitOfWork)
    {
        _warehouseRepository = warehouseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteWarehouseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var warehouseId = WarehouseId.Create(request.Id);
            var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId);

            if (warehouse == null)
            {
                return Result.Failure("WAREHOUSE_NOT_FOUND", "Warehouse not found");
            }

            if (warehouse.IsDeleted)
            {
                return Result.Failure("WAREHOUSE_ALREADY_DELETED", "Warehouse is already deleted");
            }

            var deletedBy = UserId.Create(request.DeletedBy);
            warehouse.SoftDelete(deletedBy);

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