using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Application.Queries.Refunds;

/// <summary>
/// Query to get sale information by reference number
/// </summary>
public class GetSaleInfoQuery : IQuery<Result<SaleInfoDto>>
{
    public string ReferenceNumber { get; set; } = string.Empty;
}

/// <summary>
/// Validator for GetSaleInfoQuery
/// </summary>
public class GetSaleInfoQueryValidator : AbstractValidator<GetSaleInfoQuery>
{
    public GetSaleInfoQueryValidator()
    {
        RuleFor(x => x.ReferenceNumber)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Reference number is required and must not exceed 100 characters");
    }
}

/// <summary>
/// Handler for GetSaleInfoQuery
/// </summary>
public class GetSaleInfoQueryHandler : IQueryHandler<GetSaleInfoQuery, Result<SaleInfoDto>>
{
    private readonly IStockMovementRepository _stockMovementRepository;

    public GetSaleInfoQueryHandler(IStockMovementRepository stockMovementRepository)
    {
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task<Result<SaleInfoDto>> Handle(GetSaleInfoQuery request, CancellationToken cancellationToken)
    {
        // Get all movements with the reference number
        var movements = await _stockMovementRepository.GetMovementsByReferenceAsync(request.ReferenceNumber, cancellationToken);
        
        if (!movements.Any())
        {
            return Result<SaleInfoDto>.Failure("SALE_NOT_FOUND", 
                $"No sale found with reference number: {request.ReferenceNumber}");
        }

        // Filter to sale movements
        var saleMovements = movements.Where(m => 
            m.Type == Domain.Enums.MovementType.Sale && 
            m.Quantity < 0) // Sale movements have negative quantities
            .ToList();

        if (!saleMovements.Any())
        {
            return Result<SaleInfoDto>.Failure("SALE_NOT_FOUND", 
                $"No sale movements found with reference number: {request.ReferenceNumber}");
        }

        // Get the first sale movement for basic info
        var firstSaleMovement = saleMovements.OrderBy(m => m.TimestampUtc).First();

        // Calculate total sale quantity (convert negative to positive)
        var totalSaleQuantity = Math.Abs(saleMovements.Sum(m => m.Quantity));

        // Get existing refunds for this sale reference
        var existingRefunds = movements.Where(m => 
            m.Type == Domain.Enums.MovementType.Refund && 
            m.Quantity > 0) // Refund movements have positive quantities
            .ToList();

        var totalRefunded = existingRefunds.Sum(m => m.Quantity);
        var remainingRefundable = totalSaleQuantity - totalRefunded;

        var saleInfoDto = new SaleInfoDto
        {
            ReferenceNumber = request.ReferenceNumber,
            VariantId = firstSaleMovement.InventoryItem.VariantId.Value,
            VariantSku = "Unknown", // Would need to join with variant data
            VariantName = "Unknown", // Would need to join with variant data
            WarehouseId = firstSaleMovement.InventoryItem.WarehouseId.Value,
            WarehouseName = "Unknown", // Would need to join with warehouse data
            Quantity = totalSaleQuantity,
            SaleDate = firstSaleMovement.TimestampUtc,
            ProcessedByName = "Unknown", // Would need to join with user data
            TotalRefunded = totalRefunded,
            RemainingRefundable = remainingRefundable
        };

        return Result<SaleInfoDto>.Success(saleInfoDto);
    }
}