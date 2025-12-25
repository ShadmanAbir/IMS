using FluentValidation;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Commands.StockMovements;

/// <summary>
/// Command to record a manual stock adjustment (positive or negative)
/// </summary>
public class AdjustmentCommand : ICommand<Result>
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
/// Validator for AdjustmentCommand
/// </summary>
public class AdjustmentCommandValidator : AbstractValidator<AdjustmentCommand>
{
    public AdjustmentCommandValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("WarehouseId is required");

        RuleFor(x => x.Quantity)
            .NotEqual(0)
            .WithMessage("Adjustment quantity cannot be zero");

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
/// Handler for AdjustmentCommand
/// </summary>
public class AdjustmentCommandHandler : ICommandHandler<AdjustmentCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdjustmentCommandHandler(IInventoryRepository inventoryRepository, IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AdjustmentCommand request, CancellationToken cancellationToken)
    {
        var variantId = VariantId.Create(request.VariantId);
        var warehouseId = WarehouseId.Create(request.WarehouseId);
        var actorId = UserId.Create(request.ActorId);

        // Get existing inventory item
        var inventoryItem = await _inventoryRepository.GetByVariantAndWarehouseAsync(variantId, warehouseId);
        if (inventoryItem == null)
        {
            return Result.Failure("INVENTORY_NOT_FOUND", "Inventory item not found");
        }

        // Check if negative adjustment would result in negative stock (if not allowed)
        if (request.Quantity < 0 && !inventoryItem.AllowNegativeStock)
        {
            var newBalance = inventoryItem.TotalStock + request.Quantity;
            if (newBalance < 0)
            {
                return Result.Failure("NEGATIVE_STOCK_NOT_ALLOWED", 
                    $"Adjustment would result in negative stock. Current: {inventoryItem.TotalStock}, Adjustment: {request.Quantity}");
            }
        }

        try
        {
            // Create metadata if provided
            MovementMetadata? metadata = null;
            if (request.Metadata != null && request.Metadata.Any())
            {
                metadata = MovementMetadata.FromDictionary(request.Metadata);
            }

            // Record adjustment
            inventoryItem.RecordAdjustment(
                request.Quantity,
                request.Reason,
                actorId,
                request.ReferenceNumber,
                metadata);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure("NEGATIVE_STOCK_NOT_ALLOWED", ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure("INVALID_QUANTITY", ex.Message);
        }
    }
}