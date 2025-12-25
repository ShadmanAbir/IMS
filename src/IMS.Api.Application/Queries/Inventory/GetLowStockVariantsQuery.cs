using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Application.Queries.Inventory;

/// <summary>
/// Query to get variants with low stock levels
/// </summary>
public class GetLowStockVariantsQuery : IQuery<Result<List<LowStockVariantDto>>>
{
    public Guid? WarehouseId { get; set; }
    public decimal? CustomThreshold { get; set; }
    public bool IncludeOutOfStock { get; set; } = true;
    public bool IncludeExpired { get; set; } = false;
    public int? MaxResults { get; set; } = 50;
}

/// <summary>
/// Validator for GetLowStockVariantsQuery
/// </summary>
public class GetLowStockVariantsQueryValidator : AbstractValidator<GetLowStockVariantsQuery>
{
    public GetLowStockVariantsQueryValidator()
    {
        RuleFor(x => x.CustomThreshold)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CustomThreshold.HasValue)
            .WithMessage("Custom threshold must be non-negative");

        RuleFor(x => x.MaxResults)
            .InclusiveBetween(1, 500)
            .When(x => x.MaxResults.HasValue)
            .WithMessage("Max results must be between 1 and 500");
    }
}

/// <summary>
/// Handler for GetLowStockVariantsQuery
/// </summary>
public class GetLowStockVariantsQueryHandler : IQueryHandler<GetLowStockVariantsQuery, Result<List<LowStockVariantDto>>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetLowStockVariantsQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<Result<List<LowStockVariantDto>>> Handle(GetLowStockVariantsQuery request, CancellationToken cancellationToken)
    {
        // Create a query object for the repository
        var query = new LowStockQuery
        {
            WarehouseId = request.WarehouseId.HasValue ? Domain.ValueObjects.WarehouseId.Create(request.WarehouseId.Value) : null,
            CustomThreshold = request.CustomThreshold,
            IncludeOutOfStock = request.IncludeOutOfStock,
            IncludeExpired = request.IncludeExpired,
            MaxResults = request.MaxResults ?? 50
        };

        var lowStockVariants = await _inventoryRepository.GetLowStockVariantsAsync(query);

        var dtos = lowStockVariants.Select(variant => new LowStockVariantDto
        {
            VariantId = variant.VariantId,
            SKU = variant.SKU,
            VariantName = variant.VariantName,
            ProductName = variant.ProductName,
            WarehouseId = variant.WarehouseId,
            WarehouseName = variant.WarehouseName,
            CurrentStock = variant.CurrentStock,
            ReservedStock = variant.ReservedStock,
            AvailableStock = variant.AvailableStock,
            LowStockThreshold = variant.LowStockThreshold,
            BaseUnit = variant.BaseUnit,
            ExpiryDate = variant.ExpiryDate,
            IsExpired = variant.IsExpired,
            DaysUntilExpiry = variant.DaysUntilExpiry,
            LastMovementUtc = variant.LastMovementUtc
        }).ToList();

        return Result<List<LowStockVariantDto>>.Success(dtos);
    }
}

/// <summary>
/// Query object for low stock repository method
/// </summary>
public class LowStockQuery
{
    public Domain.ValueObjects.WarehouseId? WarehouseId { get; set; }
    public decimal? CustomThreshold { get; set; }
    public bool IncludeOutOfStock { get; set; } = true;
    public bool IncludeExpired { get; set; } = false;
    public int MaxResults { get; set; } = 50;
}

/// <summary>
/// Result object for low stock variants (used by repository)
/// </summary>
public class LowStockVariantResult
{
    public Guid VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReservedStock { get; set; }
    public decimal AvailableStock { get; set; }
    public decimal LowStockThreshold { get; set; }
    public string BaseUnit { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public bool IsExpired { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public DateTime LastMovementUtc { get; set; }
}