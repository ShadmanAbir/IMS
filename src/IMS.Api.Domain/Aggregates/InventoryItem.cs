using IMS.Api.Domain.Common;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Aggregates;

/// <summary>
/// Inventory item aggregate root representing stock levels and movements for a specific variant in a warehouse.
/// Manages stock quantities, reservations, and maintains an immutable audit trail of all stock movements.
/// Supports both positive and negative stock scenarios based on configuration.
/// </summary>
public class InventoryItem : SoftDeletableAggregateRoot<InventoryItemId>
{
    private readonly List<StockMovement> _movements = new();

    /// <summary>
    /// Gets the variant identifier this inventory item tracks
    /// </summary>
    public VariantId VariantId { get; private set; }
    
    /// <summary>
    /// Gets the warehouse identifier where this inventory is located
    /// </summary>
    public WarehouseId WarehouseId { get; private set; }
    
    /// <summary>
    /// Gets the total stock quantity in base units
    /// </summary>
    public decimal TotalStock { get; private set; }
    
    /// <summary>
    /// Gets the reserved stock quantity in base units
    /// </summary>
    public decimal ReservedStock { get; private set; }
    
    /// <summary>
    /// Gets whether negative stock levels are allowed for this inventory item
    /// </summary>
    public bool AllowNegativeStock { get; private set; }
    
    /// <summary>
    /// Gets the optional expiry date for perishable items (null for non-perishable items)
    /// </summary>
    public DateTime? ExpiryDate { get; private set; }
    
    /// <summary>
    /// Gets the UTC timestamp when the inventory was last updated
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the available stock quantity (total minus reserved) in base units
    /// </summary>
    public decimal AvailableStock => TotalStock - ReservedStock;
    
    /// <summary>
    /// Gets the read-only collection of stock movements for this inventory item
    /// </summary>
    public IReadOnlyList<StockMovement> Movements => _movements.AsReadOnly();

    // Private constructor for domain logic
    private InventoryItem(
        InventoryItemId id,
        VariantId variantId,
        WarehouseId warehouseId,
        bool allowNegativeStock = false,
        DateTime? expiryDate = null) : base(id)
    {
        VariantId = variantId;
        WarehouseId = warehouseId;
        TotalStock = 0;
        ReservedStock = 0;
        AllowNegativeStock = allowNegativeStock;
        ExpiryDate = expiryDate;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new inventory item for the specified variant and warehouse
    /// </summary>
    /// <param name="variantId">The variant identifier (required)</param>
    /// <param name="warehouseId">The warehouse identifier (required)</param>
    /// <param name="allowNegativeStock">Whether to allow negative stock levels</param>
    /// <param name="expiryDate">Optional expiry date for perishable items</param>
    /// <returns>A new InventoryItem instance with zero stock</returns>
    /// <exception cref="ArgumentNullException">Thrown when variantId or warehouseId is null</exception>
    /// <exception cref="ArgumentException">Thrown when expiry date is in the past</exception>
    public static InventoryItem Create(VariantId variantId, WarehouseId warehouseId, bool allowNegativeStock = false, DateTime? expiryDate = null)
    {
        if (variantId == null)
            throw new ArgumentNullException(nameof(variantId));

        if (warehouseId == null)
            throw new ArgumentNullException(nameof(warehouseId));

        if (expiryDate.HasValue && expiryDate.Value <= DateTime.UtcNow)
            throw new ArgumentException("Expiry date cannot be in the past", nameof(expiryDate));

        return new InventoryItem(InventoryItemId.CreateNew(), variantId, warehouseId, allowNegativeStock, expiryDate);
    }

    /// <summary>
    /// Sets the opening balance for this inventory item. Can only be called once per item.
    /// </summary>
    /// <param name="quantity">The opening balance quantity (must be non-negative)</param>
    /// <param name="reason">The reason for setting the opening balance</param>
    /// <param name="actorId">The user setting the opening balance</param>
    /// <param name="referenceNumber">Optional reference number for the transaction</param>
    /// <exception cref="InvalidOperationException">Thrown when opening balance already exists</exception>
    /// <exception cref="ArgumentException">Thrown when quantity is negative</exception>
    public void SetOpeningBalance(decimal quantity, string reason, UserId actorId, string referenceNumber = null)
    {
        if (HasOpeningBalance())
            throw new InvalidOperationException("Opening balance has already been set for this inventory item");

        if (quantity < 0)
            throw new ArgumentException("Opening balance quantity cannot be negative", nameof(quantity));

        var movement = StockMovement.Create(
            MovementType.OpeningBalance,
            quantity,
            quantity, // Running balance equals quantity for opening balance
            reason,
            actorId,
            referenceNumber);

        _movements.Add(movement);
        TotalStock = quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordPurchase(decimal quantity, string reason, UserId actorId, string referenceNumber = null, MovementMetadata metadata = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Purchase quantity must be positive", nameof(quantity));

        var newBalance = TotalStock + quantity;
        var movement = StockMovement.Create(
            MovementType.Purchase,
            quantity,
            newBalance,
            reason,
            actorId,
            referenceNumber,
            metadata);

        _movements.Add(movement);
        TotalStock = newBalance;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordSale(decimal quantity, string reason, UserId actorId, string referenceNumber = null, MovementMetadata metadata = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Sale quantity must be positive", nameof(quantity));

        var negativeQuantity = -quantity;
        var newBalance = TotalStock + negativeQuantity;

        if (!AllowNegativeStock && newBalance < 0)
            throw new InvalidOperationException($"Insufficient stock. Available: {AvailableStock}, Requested: {quantity}");

        var movement = StockMovement.Create(
            MovementType.Sale,
            negativeQuantity,
            newBalance,
            reason,
            actorId,
            referenceNumber,
            metadata);

        _movements.Add(movement);
        TotalStock = newBalance;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordRefund(decimal quantity, string reason, UserId actorId, string originalSaleReference, MovementMetadata metadata = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Refund quantity must be positive", nameof(quantity));

        if (string.IsNullOrWhiteSpace(originalSaleReference))
            throw new ArgumentException("Original sale reference is required for refunds", nameof(originalSaleReference));

        var newBalance = TotalStock + quantity;
        var movement = StockMovement.Create(
            MovementType.Refund,
            quantity,
            newBalance,
            reason,
            actorId,
            originalSaleReference,
            metadata);

        _movements.Add(movement);
        TotalStock = newBalance;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordAdjustment(decimal quantity, string reason, UserId actorId, string referenceNumber = null, MovementMetadata metadata = null)
    {
        if (quantity == 0)
            throw new ArgumentException("Adjustment quantity cannot be zero", nameof(quantity));

        var newBalance = TotalStock + quantity;

        if (!AllowNegativeStock && newBalance < 0)
            throw new InvalidOperationException($"Adjustment would result in negative stock. Current: {TotalStock}, Adjustment: {quantity}");

        var movement = StockMovement.Create(
            MovementType.Adjustment,
            quantity,
            newBalance,
            reason,
            actorId,
            referenceNumber,
            metadata);

        _movements.Add(movement);
        TotalStock = newBalance;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordWriteOff(decimal quantity, string reason, UserId actorId, string referenceNumber = null, MovementMetadata metadata = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Write-off quantity must be positive", nameof(quantity));

        var negativeQuantity = -quantity;
        var newBalance = TotalStock + negativeQuantity;

        if (!AllowNegativeStock && newBalance < 0)
            throw new InvalidOperationException($"Insufficient stock for write-off. Available: {TotalStock}, Requested: {quantity}");

        var movement = StockMovement.Create(
            MovementType.WriteOff,
            negativeQuantity,
            newBalance,
            reason,
            actorId,
            referenceNumber,
            metadata);

        _movements.Add(movement);
        TotalStock = newBalance;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordTransferOut(decimal quantity, string reason, UserId actorId, WarehouseId destinationWarehouseId, string referenceNumber = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Transfer quantity must be positive", nameof(quantity));

        var negativeQuantity = -quantity;
        var newBalance = TotalStock + negativeQuantity;

        if (!AllowNegativeStock && newBalance < 0)
            throw new InvalidOperationException($"Insufficient stock for transfer. Available: {TotalStock}, Requested: {quantity}");

        var metadata = MovementMetadata.FromTransfer(WarehouseId, destinationWarehouseId);
        var movement = StockMovement.Create(
            MovementType.Transfer,
            negativeQuantity,
            newBalance,
            reason,
            actorId,
            referenceNumber,
            metadata);

        _movements.Add(movement);
        TotalStock = newBalance;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordTransferIn(decimal quantity, string reason, UserId actorId, WarehouseId sourceWarehouseId, string referenceNumber = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Transfer quantity must be positive", nameof(quantity));

        var newBalance = TotalStock + quantity;
        var metadata = MovementMetadata.FromTransfer(sourceWarehouseId, WarehouseId);
        var movement = StockMovement.Create(
            MovementType.Transfer,
            quantity,
            newBalance,
            reason,
            actorId,
            referenceNumber,
            metadata);

        _movements.Add(movement);
        TotalStock = newBalance;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReserveStock(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Reservation quantity must be positive", nameof(quantity));

        if (AvailableStock < quantity)
            throw new InvalidOperationException($"Insufficient available stock for reservation. Available: {AvailableStock}, Requested: {quantity}");

        ReservedStock += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReleaseReservation(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Release quantity must be positive", nameof(quantity));

        if (ReservedStock < quantity)
            throw new InvalidOperationException($"Cannot release more than reserved. Reserved: {ReservedStock}, Requested: {quantity}");

        ReservedStock -= quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReleaseReservedStock(decimal quantity, ReservationId reservationId)
    {
        ReleaseReservation(quantity);
    }

    public void ReserveStock(decimal quantity, ReservationId reservationId)
    {
        ReserveStock(quantity);
    }

    public void UpdateNegativeStockPolicy(bool allowNegativeStock)
    {
        AllowNegativeStock = allowNegativeStock;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool HasOpeningBalance()
    {
        return _movements.Any(m => m.Type == MovementType.OpeningBalance);
    }

    public bool IsOutOfStock()
    {
        return TotalStock <= 0;
    }

    public bool HasAvailableStock()
    {
        return AvailableStock > 0;
    }

    public StockMovement GetLastMovement()
    {
        return _movements.OrderByDescending(m => m.TimestampUtc).FirstOrDefault();
    }

    public IEnumerable<StockMovement> GetMovementsByType(MovementType type)
    {
        return _movements.Where(m => m.Type == type).OrderByDescending(m => m.TimestampUtc);
    }

    /// <summary>
    /// Updates the expiry date for this inventory item
    /// </summary>
    /// <param name="expiryDate">The new expiry date (null for non-perishable items)</param>
    /// <exception cref="ArgumentException">Thrown when expiry date is in the past</exception>
    public void UpdateExpiryDate(DateTime? expiryDate)
    {
        if (expiryDate.HasValue && expiryDate.Value <= DateTime.UtcNow)
            throw new ArgumentException("Expiry date cannot be in the past", nameof(expiryDate));

        ExpiryDate = expiryDate;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the inventory item is expired
    /// </summary>
    /// <returns>True if the item has an expiry date and it has passed</returns>
    public bool IsExpired()
    {
        return ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the inventory item is near expiry (within specified days)
    /// </summary>
    /// <param name="daysThreshold">Number of days to consider as "near expiry"</param>
    /// <returns>True if the item expires within the specified threshold</returns>
    public bool IsNearExpiry(int daysThreshold = 7)
    {
        if (!ExpiryDate.HasValue)
            return false;

        var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);
        return ExpiryDate.Value <= thresholdDate;
    }

    /// <summary>
    /// Gets the number of days until expiry (negative if already expired)
    /// </summary>
    /// <returns>Days until expiry, or null if no expiry date is set</returns>
    public int? GetDaysUntilExpiry()
    {
        if (!ExpiryDate.HasValue)
            return null;

        return (int)(ExpiryDate.Value - DateTime.UtcNow).TotalDays;
    }

    /// <summary>
    /// Gets stock movements within a specific date range
    /// </summary>
    /// <param name="startUtc">Start date (UTC)</param>
    /// <param name="endUtc">End date (UTC)</param>
    /// <returns>Stock movements within the date range, ordered by timestamp descending</returns>
    public IEnumerable<StockMovement> GetMovementsByDateRange(DateTime startUtc, DateTime endUtc)
    {
        return _movements.Where(m => m.TimestampUtc >= startUtc && m.TimestampUtc <= endUtc)
                        .OrderByDescending(m => m.TimestampUtc);
    }
}