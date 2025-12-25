using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Commands.StockMovements;

/// <summary>
/// Command to record a warehouse-to-warehouse stock transfer
/// </summary>
public class TransferCommand : ICommand<Result>
{
    public Guid VariantId { get; set; }
    public Guid SourceWarehouseId { get; set; }
    public Guid DestinationWarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public string? ReferenceNumber { get; set; }
}

/// <summary>
/// Validator for TransferCommand
/// </summary>
public class TransferCommandValidator : AbstractValidator<TransferCommand>
{
    public TransferCommandValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required");

        RuleFor(x => x.SourceWarehouseId)
            .NotEmpty()
            .WithMessage("SourceWarehouseId is required");

        RuleFor(x => x.DestinationWarehouseId)
            .NotEmpty()
            .WithMessage("DestinationWarehouseId is required");

        RuleFor(x => x.DestinationWarehouseId)
            .NotEqual(x => x.SourceWarehouseId)
            .WithMessage("Source and destination warehouses must be different");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Transfer quantity must be positive");

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
/// Handler for TransferCommand
/// </summary>
public class TransferCommandHandler : ICommandHandler<TransferCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransferCommandHandler(IInventoryRepository inventoryRepository, IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(TransferCommand request, CancellationToken cancellationToken)
    {
        var variantId = VariantId.Create(request.VariantId);
        var sourceWarehouseId = WarehouseId.Create(request.SourceWarehouseId);
        var destinationWarehouseId = WarehouseId.Create(request.DestinationWarehouseId);
        var actorId = UserId.Create(request.ActorId);

        // Generate reference number if not provided
        var referenceNumber = request.ReferenceNumber ?? $"TRF-{Guid.NewGuid():N}";

        // Get source inventory item
        var sourceInventory = await _inventoryRepository.GetByVariantAndWarehouseAsync(variantId, sourceWarehouseId);
        if (sourceInventory == null)
        {
            return Result.Failure("SOURCE_INVENTORY_NOT_FOUND", "Source inventory item not found");
        }

        // Check available stock in source
        if (sourceInventory.AvailableStock < request.Quantity && !sourceInventory.AllowNegativeStock)
        {
            return Result.Failure("INSUFFICIENT_STOCK", 
                $"Insufficient stock in source warehouse. Available: {sourceInventory.AvailableStock}, Requested: {request.Quantity}");
        }

        // Get or create destination inventory item
        var destinationInventory = await _inventoryRepository.GetByVariantAndWarehouseAsync(variantId, destinationWarehouseId);
        if (destinationInventory == null)
        {
            // Create destination inventory item with same settings as source
            destinationInventory = Domain.Aggregates.InventoryItem.Create(
                variantId, 
                destinationWarehouseId, 
                sourceInventory.AllowNegativeStock,
                sourceInventory.ExpiryDate);
            
            await _inventoryRepository.AddAsync(destinationInventory);
        }

        try
        {
            // Record transfer out from source
            sourceInventory.RecordTransferOut(
                request.Quantity,
                request.Reason,
                actorId,
                destinationWarehouseId,
                referenceNumber);

            // Record transfer in to destination
            destinationInventory.RecordTransferIn(
                request.Quantity,
                request.Reason,
                actorId,
                sourceWarehouseId,
                referenceNumber);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure("INSUFFICIENT_STOCK", ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure("INVALID_QUANTITY", ex.Message);
        }
    }
}