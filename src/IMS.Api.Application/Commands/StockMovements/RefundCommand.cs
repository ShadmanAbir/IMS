using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Commands.StockMovements;

/// <summary>
/// Command to process a refund (stock increase with original sale validation)
/// </summary>
public class RefundCommand : ICommand<Result>
{
    public Guid VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public string OriginalSaleReference { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Validator for RefundCommand
/// </summary>
public class RefundCommandValidator : AbstractValidator<RefundCommand>
{
    public RefundCommandValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("WarehouseId is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Refund quantity must be positive");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Reason is required and must not exceed 255 characters");

        RuleFor(x => x.ActorId)
            .NotEmpty()
            .WithMessage("ActorId is required");

        RuleFor(x => x.OriginalSaleReference)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Original sale reference is required and must not exceed 100 characters");
    }
}

/// <summary>
/// Handler for RefundCommand
/// </summary>
public class RefundCommandHandler : ICommandHandler<RefundCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RefundCommandHandler(
        IInventoryRepository inventoryRepository,
        IStockMovementRepository stockMovementRepository,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _stockMovementRepository = stockMovementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RefundCommand request, CancellationToken cancellationToken)
    {
        var variantId = VariantId.Create(request.VariantId);
        var warehouseId = WarehouseId.Create(request.WarehouseId);
        var actorId = UserId.Create(request.ActorId);

        // Validate original sale exists and get sale details
        var originalSaleValidation = await ValidateOriginalSaleAsync(
            request.OriginalSaleReference, 
            variantId, 
            warehouseId, 
            request.Quantity, 
            cancellationToken);

        if (!originalSaleValidation.IsSuccess)
        {
            return originalSaleValidation;
        }

        // Get existing inventory item
        var inventoryItem = await _inventoryRepository.GetByVariantAndWarehouseAsync(variantId, warehouseId, cancellationToken);
        if (inventoryItem == null)
        {
            return Result.Failure("INVENTORY_NOT_FOUND", "Inventory item not found");
        }

        try
        {
            // Create metadata if provided
            MovementMetadata? metadata = null;
            if (request.Metadata != null && request.Metadata.Any())
            {
                metadata = MovementMetadata.FromDictionary(request.Metadata);
            }

            // Record refund
            inventoryItem.RecordRefund(
                request.Quantity,
                request.Reason,
                actorId,
                request.OriginalSaleReference,
                metadata);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure("INVALID_REFUND", ex.Message);
        }
    }

    /// <summary>
    /// Validates that the original sale exists and the refund quantity is valid
    /// </summary>
    private async Task<Result> ValidateOriginalSaleAsync(
        string originalSaleReference, 
        VariantId variantId, 
        WarehouseId warehouseId, 
        decimal refundQuantity,
        CancellationToken cancellationToken)
    {
        // Get all movements with the original sale reference
        var movements = await _stockMovementRepository.GetMovementsByReferenceAsync(originalSaleReference, cancellationToken);
        
        if (!movements.Any())
        {
            return Result.Failure("ORIGINAL_SALE_NOT_FOUND", 
                $"No sale found with reference number: {originalSaleReference}");
        }

        // Filter to sale movements for the specific variant and warehouse
        var saleMovements = movements.Where(m => 
            m.Type == Domain.Enums.MovementType.Sale && 
            m.Quantity < 0) // Sale movements have negative quantities
            .ToList();

        if (!saleMovements.Any())
        {
            return Result.Failure("ORIGINAL_SALE_NOT_FOUND", 
                $"No sale movements found with reference number: {originalSaleReference}");
        }

        // Calculate total sale quantity (convert negative to positive for comparison)
        var totalSaleQuantity = Math.Abs(saleMovements.Sum(m => m.Quantity));

        // Get existing refunds for this sale reference
        var existingRefunds = movements.Where(m => 
            m.Type == Domain.Enums.MovementType.Refund && 
            m.Quantity > 0) // Refund movements have positive quantities
            .ToList();

        var totalRefundedQuantity = existingRefunds.Sum(m => m.Quantity);

        // Check if refund quantity exceeds remaining refundable amount
        var remainingRefundableQuantity = totalSaleQuantity - totalRefundedQuantity;
        
        if (refundQuantity > remainingRefundableQuantity)
        {
            return Result.Failure("REFUND_EXCEEDS_SALE", 
                $"Refund quantity ({refundQuantity}) exceeds remaining refundable amount ({remainingRefundableQuantity}). " +
                $"Original sale: {totalSaleQuantity}, Already refunded: {totalRefundedQuantity}");
        }

        return Result.Success();
    }
}