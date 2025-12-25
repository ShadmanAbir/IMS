using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Commands.StockMovements;

/// <summary>
/// Command to record a purchase (stock increase)
/// </summary>
public class PurchaseCommand : ICommand<Result>
{
    public Guid VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public string? ReferenceNumber { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Validator for PurchaseCommand
/// </summary>
public class PurchaseCommandValidator : AbstractValidator<PurchaseCommand>
{
    public PurchaseCommandValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("WarehouseId is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Purchase quantity must be positive");

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
    }
}

/// <summary>
/// Handler for PurchaseCommand
/// </summary>
public class PurchaseCommandHandler : ICommandHandler<PurchaseCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseCommandHandler(IInventoryRepository inventoryRepository, IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(PurchaseCommand request, CancellationToken cancellationToken)
    {
        var variantId = VariantId.Create(request.VariantId);
        var warehouseId = WarehouseId.Create(request.WarehouseId);
        var actorId = UserId.Create(request.ActorId);

        // Get existing inventory item
        var inventoryItem = await _inventoryRepository.GetByVariantAndWarehouseAsync(variantId, warehouseId);
        if (inventoryItem == null)
        {
            return Result.Failure("INVENTORY_NOT_FOUND", "Inventory item not found. Please set opening balance first.");
        }

        try
        {
            // Create metadata if provided
            MovementMetadata? metadata = null;
            if (request.Metadata != null && request.Metadata.Any())
            {
                metadata = MovementMetadata.FromDictionary(request.Metadata);
            }

            // Record purchase
            inventoryItem.RecordPurchase(
                request.Quantity,
                request.Reason,
                actorId,
                request.ReferenceNumber,
                metadata);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure("INVALID_QUANTITY", ex.Message);
        }
    }
}