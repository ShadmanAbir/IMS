using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Application.Queries.Inventory;

/// <summary>
/// Query to get inventory levels for multiple variants
/// </summary>
public class GetBulkInventoryLevelsQuery : IQuery<Result<List<InventoryItemDto>>>
{
    public List<Guid> VariantIds { get; set; } = new();
    public Guid? WarehouseId { get; set; }
    public bool IncludeExpired { get; set; } = true;
    public bool IncludeOutOfStock { get; set; } = true;
}

/// <summary>
/// Validator for GetBulkInventoryLevelsQuery
/// </summary>
public class GetBulkInventoryLevelsQueryValidator : AbstractValidator<GetBulkInventoryLevelsQuery>
{
    public GetBulkInventoryLevelsQueryValidator()
    {
        RuleFor(x => x.VariantIds)
            .NotEmpty()
            .WithMessage("At least one VariantId is required");

        RuleFor(x => x.VariantIds)
            .Must(ids => ids.Count <= 100)
            .WithMessage("Maximum 100 variants can be queried at once");

        RuleForEach(x => x.VariantIds)
            .NotEmpty()
            .WithMessage("VariantId cannot be empty");
    }
}

/// <summary>
/// Handler for GetBulkInventoryLevelsQuery
/// </summary>
public class GetBulkInventoryLevelsQueryHandler : IQueryHandler<GetBulkInventoryLevelsQuery, Result<List<InventoryItemDto>>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetBulkInventoryLevelsQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<Result<List<InventoryItemDto>>> Handle(GetBulkInventoryLevelsQuery request, CancellationToken cancellationToken)
    {
        var variantIds = request.VariantIds.Select(id => Domain.ValueObjects.VariantId.Create(id)).ToList();
        var warehouseId = request.WarehouseId.HasValue ? Domain.ValueObjects.WarehouseId.Create(request.WarehouseId.Value) : null;

        var inventoryItems = await _inventoryRepository.GetBulkInventoryAsync(variantIds, warehouseId);

        var dtos = inventoryItems
            .Where(item => request.IncludeExpired || !item.IsExpired())
            .Where(item => request.IncludeOutOfStock || !item.IsOutOfStock())
            .Select(item => new InventoryItemDto
            {
                Id = item.Id.Value,
                VariantId = item.VariantId.Value,
                WarehouseId = item.WarehouseId.Value,
                TotalStock = item.TotalStock,
                ReservedStock = item.ReservedStock,
                AllowNegativeStock = item.AllowNegativeStock,
                ExpiryDate = item.ExpiryDate,
                UpdatedAtUtc = item.UpdatedAtUtc,
                IsDeleted = item.IsDeleted,
                DeletedAtUtc = item.DeletedAtUtc,
                DeletedBy = item.DeletedBy?.Value
            })
            .ToList();

        return Result<List<InventoryItemDto>>.Success(dtos);
    }
}