using IMS.Api.Application.Common.Models;
using IMS.Api.Application.Queries.Inventory;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Application.Common.Interfaces;

public interface IStockMovementRepository
{
    Task<StockMovement?> GetByIdAsync(StockMovementId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockMovement>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(StockMovement entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(StockMovement entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(StockMovement entity, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    Task<PagedResult<StockMovement>> GetMovementHistoryAsync(StockMovementQuery query, CancellationToken cancellationToken = default);
    Task<List<StockMovement>> GetMovementsByReferenceAsync(string referenceNumber, CancellationToken cancellationToken = default);
    Task<List<StockMovement>> GetMovementsByInventoryItemAsync(InventoryItemId inventoryItemId, CancellationToken cancellationToken = default);
    Task<List<StockMovement>> GetMovementsByVariantAsync(VariantId variantId, CancellationToken cancellationToken = default);
    Task<List<StockMovement>> GetMovementsByTypeAsync(MovementType movementType, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<List<StockMovement>> GetRecentMovementsAsync(int count = 100, CancellationToken cancellationToken = default);
}