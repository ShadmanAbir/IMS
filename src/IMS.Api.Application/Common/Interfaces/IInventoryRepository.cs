using IMS.Api.Application.Queries.Inventory;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Common.Interfaces;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByIdAsync(InventoryItemId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(InventoryItem entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(InventoryItem entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(InventoryItem entity, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    Task<InventoryItem?> GetByVariantAndWarehouseAsync(VariantId variantId, WarehouseId warehouseId, CancellationToken cancellationToken = default);
    Task<List<InventoryItem>> GetBulkInventoryAsync(List<VariantId> variantIds, WarehouseId? warehouseId, CancellationToken cancellationToken = default);
    Task<bool> HasOpeningBalanceAsync(VariantId variantId, WarehouseId warehouseId, CancellationToken cancellationToken = default);
    Task<List<InventoryItem>> GetLowStockItemsAsync(WarehouseId? warehouseId = null, CancellationToken cancellationToken = default);
    Task<List<InventoryItem>> GetByWarehouseAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default);
    Task<List<LowStockVariantResult>> GetLowStockVariantsAsync(LowStockQuery query, CancellationToken cancellationToken = default);
}