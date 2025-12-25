using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Application.Queries.Inventory;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Infrastructure.Examples;

/// <summary>
/// Example showing how to use the simplified Dapper-based repositories
/// </summary>
public class DapperUsageExample
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStockMovementRepository _stockMovementRepository;

    public DapperUsageExample(
        IInventoryRepository inventoryRepository,
        IStockMovementRepository stockMovementRepository)
    {
        _inventoryRepository = inventoryRepository;
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task ExampleUsage()
    {
        // Create sample IDs
        var variantId = VariantId.CreateNew();
        var warehouseId = WarehouseId.CreateNew();

        // Get inventory by variant and warehouse
        var inventoryItem = await _inventoryRepository.GetByVariantAndWarehouseAsync(variantId, warehouseId);

        if (inventoryItem == null)
        {
            // Create new inventory item
            inventoryItem = InventoryItem.Create(variantId, warehouseId, allowNegativeStock: false);
            await _inventoryRepository.AddAsync(inventoryItem);
        }

        // Get bulk inventory for multiple variants
        var variantIds = new List<VariantId> { variantId, VariantId.CreateNew() };
        var bulkInventory = await _inventoryRepository.GetBulkInventoryAsync(variantIds, warehouseId);

        // Get low stock items
        var lowStockItems = await _inventoryRepository.GetLowStockItemsAsync(warehouseId);

        // Get recent stock movements
        var recentMovements = await _stockMovementRepository.GetRecentMovementsAsync(50);

        // Query stock movements with filters
        var query = new StockMovementQuery
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            PageNumber = 1,
            PageSize = 20
        };
        var movementHistory = await _stockMovementRepository.GetMovementHistoryAsync(query);

        // The repositories now use Dapper for direct SQL queries with AutoMapper for object mapping
        // This provides better performance and more control over SQL queries while maintaining
        // the domain model integrity
    }
}