using FluentValidation;
using IMS.Api.Application.Common.DTOs;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Common.Models;

namespace IMS.Api.Application.Queries.Inventory;

/// <summary>
/// Query to get inventory level for a single variant in a warehouse
/// </summary>
public class GetInventoryLevelQuery : IQuery<Result<InventoryItemDto>>
{
    public Guid VariantId { get; set; }
    public Guid WarehouseId { get; set; }
}

/// <summary>
/// Validator for GetInventoryLevelQuery
/// </summary>
public class GetInventoryLevelQueryValidator : AbstractValidator<GetInventoryLevelQuery>
{
    public GetInventoryLevelQueryValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty()
            .WithMessage("VariantId is required");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("WarehouseId is required");
    }
}

/// <summary>
/// Handler for GetInventoryLevelQuery
/// </summary>
public class GetInventoryLevelQueryHandler : IQueryHandler<GetInventoryLevelQuery, Result<InventoryItemDto>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetInventoryLevelQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<Result<InventoryItemDto>> Handle(GetInventoryLevelQuery request, CancellationToken cancellationToken)
    {
        var variantId = Domain.ValueObjects.VariantId.Create(request.VariantId);
        var warehouseId = Domain.ValueObjects.WarehouseId.Create(request.WarehouseId);

        var inventoryItem = await _inventoryRepository.GetByVariantAndWarehouseAsync(variantId, warehouseId);
        
        if (inventoryItem == null)
        {
            return Result<InventoryItemDto>.Failure("INVENTORY_NOT_FOUND", "Inventory item not found");
        }

        var dto = new InventoryItemDto
        {
            Id = inventoryItem.Id.Value,
            VariantId = inventoryItem.VariantId.Value,
            WarehouseId = inventoryItem.WarehouseId.Value,
            TotalStock = inventoryItem.TotalStock,
            ReservedStock = inventoryItem.ReservedStock,
            AllowNegativeStock = inventoryItem.AllowNegativeStock,
            ExpiryDate = inventoryItem.ExpiryDate,
            UpdatedAtUtc = inventoryItem.UpdatedAtUtc,
            IsDeleted = inventoryItem.IsDeleted,
            DeletedAtUtc = inventoryItem.DeletedAtUtc,
            DeletedBy = inventoryItem.DeletedBy?.Value
        };

        return Result<InventoryItemDto>.Success(dto);
    }
}