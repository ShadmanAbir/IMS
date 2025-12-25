using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Commands.StockMovements;

/// <summary>
/// Command to set the opening balance for a variant in a warehouse
/// </summary>
public class OpeningBalanceCommand : ICommand<Result>
{
    public Guid VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public string? ReferenceNumber { get; set; }
    public bool AllowNegativeStock { get; set; } = false;
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// Validator for OpeningBalanceCommand
/// </summary>
public class OpeningBalanceCommandValidator : AbstractValidator<OpeningBalanceCommand>
{
    public OpeningBalanceCommandValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("WarehouseId is required");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Opening balance quantity cannot be negative");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Reason is required and must not exceed 255 characters");

        RuleFor(x => x.ActorId)
            .NotEmpty()
            .WithMessage("ActorId is required");

        RuleFor(x => x.ReferenceNumber)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.ReferenceNumber))
            .WithMessage("Reference number must not exceed 100 characters");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ExpiryDate.HasValue)
            .WithMessage("Expiry date must be in the future");
    }
}

/// <summary>
/// Handler for OpeningBalanceCommand
/// </summary>
public class OpeningBalanceCommandHandler : ICommandHandler<OpeningBalanceCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OpeningBalanceCommandHandler(IInventoryRepository inventoryRepository, IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(OpeningBalanceCommand request, CancellationToken cancellationToken)
    {
        var variantId = VariantId.Create(request.VariantId);
        var warehouseId = WarehouseId.Create(request.WarehouseId);
        var actorId = UserId.Create(request.ActorId);

        // Check if opening balance already exists
        var existingInventory = await _inventoryRepository.GetByVariantAndWarehouseAsync(variantId, warehouseId);
        if (existingInventory != null && existingInventory.HasOpeningBalance())
        {
            return Result.Failure("OPENING_BALANCE_EXISTS", "Opening balance has already been set for this variant in this warehouse");
        }

        // Create new inventory item if it doesn't exist
        if (existingInventory == null)
        {
            existingInventory = Domain.Aggregates.InventoryItem.Create(
                variantId, 
                warehouseId, 
                request.AllowNegativeStock, 
                request.ExpiryDate);
            
            await _inventoryRepository.AddAsync(existingInventory);
        }

        try
        {
            // Set opening balance
            existingInventory.SetOpeningBalance(
                request.Quantity,
                request.Reason,
                actorId,
                request.ReferenceNumber);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure("OPENING_BALANCE_EXISTS", ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure("INVALID_QUANTITY", ex.Message);
        }
    }
}