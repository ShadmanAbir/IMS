using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Queries.Refunds;

/// <summary>
/// Query to validate a refund request against original sale
/// </summary>
public class GetRefundValidationQuery : IQuery<Result<RefundValidationDto>>
{
    public string OriginalSaleReference { get; set; } = string.Empty;
    public Guid VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal RequestedQuantity { get; set; }
}

/// <summary>
/// Validator for GetRefundValidationQuery
/// </summary>
public class GetRefundValidationQueryValidator : AbstractValidator<GetRefundValidationQuery>
{
    public GetRefundValidationQueryValidator()
    {
        RuleFor(x => x.OriginalSaleReference)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Original sale reference is required and must not exceed 100 characters");

        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("WarehouseId is required");

        RuleFor(x => x.RequestedQuantity)
            .GreaterThan(0)
            .WithMessage("Requested quantity must be positive");
    }
}

/// <summary>
/// Handler for GetRefundValidationQuery
/// </summary>
public class GetRefundValidationQueryHandler : IQueryHandler<GetRefundValidationQuery, Result<RefundValidationDto>>
{
    private readonly IStockMovementRepository _stockMovementRepository;

    public GetRefundValidationQueryHandler(IStockMovementRepository stockMovementRepository)
    {
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task<Result<RefundValidationDto>> Handle(GetRefundValidationQuery request, CancellationToken cancellationToken)
    {
        var variantId = VariantId.Create(request.VariantId);
        var warehouseId = WarehouseId.Create(request.WarehouseId);

        // Get all movements with the original sale reference
        var movements = await _stockMovementRepository.GetMovementsByReferenceAsync(request.OriginalSaleReference, cancellationToken);
        
        if (!movements.Any())
        {
            return Result<RefundValidationDto>.Failure("ORIGINAL_SALE_NOT_FOUND", 
                $"No sale found with reference number: {request.OriginalSaleReference}");
        }

        // Filter to sale movements for the specific variant and warehouse
        var saleMovements = movements.Where(m => 
            m.Type == Domain.Enums.MovementType.Sale && 
            m.Quantity < 0) // Sale movements have negative quantities
            .ToList();

        if (!saleMovements.Any())
        {
            return Result<RefundValidationDto>.Failure("ORIGINAL_SALE_NOT_FOUND", 
                $"No sale movements found with reference number: {request.OriginalSaleReference}");
        }

        // Calculate total sale quantity (convert negative to positive for comparison)
        var totalSaleQuantity = Math.Abs(saleMovements.Sum(m => m.Quantity));
        var originalSaleDate = saleMovements.Min(m => m.TimestampUtc);

        // Get existing refunds for this sale reference
        var existingRefunds = movements.Where(m => 
            m.Type == Domain.Enums.MovementType.Refund && 
            m.Quantity > 0) // Refund movements have positive quantities
            .ToList();

        var totalRefundedQuantity = existingRefunds.Sum(m => m.Quantity);
        var remainingRefundableQuantity = totalSaleQuantity - totalRefundedQuantity;

        // Build refund history
        var refundHistory = existingRefunds.Select(r => new RefundHistoryDto
        {
            Id = r.Id.Value,
            Quantity = r.Quantity,
            Reason = r.Reason,
            ProcessedAtUtc = r.TimestampUtc,
            ProcessedByName = "Unknown" // Would need to join with user data
        }).OrderByDescending(r => r.ProcessedAtUtc).ToList();

        // Validate if the requested quantity can be refunded
        var canRefund = request.RequestedQuantity <= remainingRefundableQuantity;
        var validationMessage = canRefund 
            ? "Refund can be processed" 
            : $"Refund quantity ({request.RequestedQuantity}) exceeds remaining refundable amount ({remainingRefundableQuantity})";

        var validationDto = new RefundValidationDto
        {
            OriginalSaleReference = request.OriginalSaleReference,
            OriginalSaleQuantity = totalSaleQuantity,
            TotalRefundedQuantity = totalRefundedQuantity,
            RemainingRefundableQuantity = remainingRefundableQuantity,
            OriginalSaleDate = originalSaleDate,
            RefundHistory = refundHistory,
            CanRefund = canRefund,
            ValidationMessage = validationMessage
        };

        return Result<RefundValidationDto>.Success(validationDto);
    }
}