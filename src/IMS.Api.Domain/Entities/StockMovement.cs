using IMS.Api.Domain.Common;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// Stock movement entity representing a single entry in the double-entry stock accounting system.
/// Each stock transaction creates two complementary movements (debit and credit) to maintain balance integrity.
/// Movements are immutable once created to ensure audit trail integrity.
/// </summary>
public class StockMovement : Entity<StockMovementId>
{
    /// <summary>
    /// Gets the type of stock movement (Purchase, Sale, Transfer, etc.)
    /// </summary>
    public MovementType Type { get; private set; }
    
    /// <summary>
    /// Gets the quantity moved in base units (positive for debits, negative for credits)
    /// </summary>
    public decimal Quantity { get; private set; }
    
    /// <summary>
    /// Gets the running balance after this movement in base units
    /// </summary>
    public decimal RunningBalance { get; private set; }
    
    /// <summary>
    /// Gets the reason or description for this movement
    /// </summary>
    public string Reason { get; private set; }
    
    /// <summary>
    /// Gets the user who initiated this movement
    /// </summary>
    public UserId ActorId { get; private set; }
    
    /// <summary>
    /// Gets the UTC timestamp when this movement was recorded
    /// </summary>
    public DateTime TimestampUtc { get; private set; }
    
    /// <summary>
    /// Gets the reference number linking related movements (e.g., transfer transactions)
    /// </summary>
    public string ReferenceNumber { get; private set; }
    
    /// <summary>
    /// Gets additional metadata for this movement
    /// </summary>
    public MovementMetadata Metadata { get; private set; }
    
    /// <summary>
    /// Gets the double-entry type (Debit or Credit) for accounting balance
    /// </summary>
    public DoubleEntryType EntryType { get; private set; }
    
    /// <summary>
    /// Gets the paired movement ID for double-entry transactions (null for single-entry movements)
    /// </summary>
    public StockMovementId? PairedMovementId { get; private set; }
    
    /// <summary>
    /// Gets the inventory item this movement belongs to (navigation property)
    /// </summary>
    public InventoryItemId InventoryItemId { get; private set; }
    
    /// <summary>
    /// Gets the inventory item this movement belongs to (navigation property)
    /// </summary>
    public Aggregates.InventoryItem InventoryItem { get; private set; }

    // Private constructor for domain logic
    private StockMovement(
        StockMovementId id,
        MovementType type,
        decimal quantity,
        decimal runningBalance,
        string reason,
        UserId actorId,
        string referenceNumber,
        MovementMetadata metadata,
        DoubleEntryType entryType,
        StockMovementId? pairedMovementId = null) : base(id)
    {
        Type = type;
        Quantity = quantity;
        RunningBalance = runningBalance;
        Reason = reason;
        ActorId = actorId;
        TimestampUtc = DateTime.UtcNow;
        ReferenceNumber = referenceNumber;
        Metadata = metadata;
        EntryType = entryType;
        PairedMovementId = pairedMovementId;
    }

    /// <summary>
    /// Creates a single stock movement entry (for simple transactions)
    /// </summary>
    /// <param name="type">The movement type</param>
    /// <param name="quantity">The quantity in base units</param>
    /// <param name="runningBalance">The running balance after this movement</param>
    /// <param name="reason">The reason for the movement</param>
    /// <param name="actorId">The user performing the movement</param>
    /// <param name="referenceNumber">Optional reference number</param>
    /// <param name="metadata">Optional metadata</param>
    /// <returns>A new StockMovement instance</returns>
    public static StockMovement Create(
        MovementType type,
        decimal quantity,
        decimal runningBalance,
        string reason,
        UserId actorId,
        string referenceNumber = null,
        MovementMetadata metadata = null)
    {
        if (actorId == null)
            throw new ArgumentNullException(nameof(actorId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be null or empty", nameof(reason));

        ValidateQuantityForMovementType(type, quantity);

        var entryType = DetermineEntryType(type, quantity);

        return new StockMovement(
            StockMovementId.CreateNew(),
            type,
            quantity,
            runningBalance,
            reason.Trim(),
            actorId,
            referenceNumber?.Trim(),
            metadata ?? MovementMetadata.Empty(),
            entryType);
    }

    /// <summary>
    /// Creates a pair of double-entry stock movements for transactions requiring balance validation
    /// </summary>
    /// <param name="type">The movement type</param>
    /// <param name="quantity">The absolute quantity being moved</param>
    /// <param name="sourceRunningBalance">The running balance for the source (credit) entry</param>
    /// <param name="destinationRunningBalance">The running balance for the destination (debit) entry</param>
    /// <param name="reason">The reason for the movement</param>
    /// <param name="actorId">The user performing the movement</param>
    /// <param name="referenceNumber">Reference number linking the paired movements</param>
    /// <param name="metadata">Optional metadata</param>
    /// <returns>A tuple containing the debit and credit movements</returns>
    public static (StockMovement debitEntry, StockMovement creditEntry) CreateDoubleEntry(
        MovementType type,
        decimal quantity,
        decimal sourceRunningBalance,
        decimal destinationRunningBalance,
        string reason,
        UserId actorId,
        string referenceNumber,
        MovementMetadata metadata = null)
    {
        if (actorId == null)
            throw new ArgumentNullException(nameof(actorId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be null or empty", nameof(reason));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive for double-entry movements", nameof(quantity));

        if (string.IsNullOrWhiteSpace(referenceNumber))
            throw new ArgumentException("Reference number is required for double-entry movements", nameof(referenceNumber));

        var debitId = StockMovementId.CreateNew();
        var creditId = StockMovementId.CreateNew();

        // Create credit entry (source - decreases stock)
        var creditEntry = new StockMovement(
            creditId,
            type,
            -quantity, // Negative for credit
            sourceRunningBalance,
            reason.Trim(),
            actorId,
            referenceNumber.Trim(),
            metadata ?? MovementMetadata.Empty(),
            DoubleEntryType.Credit,
            debitId);

        // Create debit entry (destination - increases stock)
        var debitEntry = new StockMovement(
            debitId,
            type,
            quantity, // Positive for debit
            destinationRunningBalance,
            reason.Trim(),
            actorId,
            referenceNumber.Trim(),
            metadata ?? MovementMetadata.Empty(),
            DoubleEntryType.Debit,
            creditId);

        return (debitEntry, creditEntry);
    }

    private static void ValidateQuantityForMovementType(MovementType type, decimal quantity)
    {
        switch (type)
        {
            case MovementType.OpeningBalance:
            case MovementType.Purchase:
            case MovementType.Refund:
                if (quantity <= 0)
                    throw new ArgumentException($"{type} quantity must be positive", nameof(quantity));
                break;

            case MovementType.Sale:
            case MovementType.WriteOff:
                if (quantity >= 0)
                    throw new ArgumentException($"{type} quantity must be negative", nameof(quantity));
                break;

            case MovementType.Adjustment:
            case MovementType.Transfer:
                // Adjustments and transfers can be positive or negative
                break;

            default:
                throw new ArgumentException($"Unknown movement type: {type}");
        }
    }

    private static DoubleEntryType DetermineEntryType(MovementType type, decimal quantity)
    {
        return quantity > 0 ? DoubleEntryType.Debit : DoubleEntryType.Credit;
    }

    /// <summary>
    /// Checks if this is a positive movement (debit entry)
    /// </summary>
    /// <returns>True if quantity is positive</returns>
    public bool IsPositiveMovement()
    {
        return Quantity > 0;
    }

    /// <summary>
    /// Checks if this is a negative movement (credit entry)
    /// </summary>
    /// <returns>True if quantity is negative</returns>
    public bool IsNegativeMovement()
    {
        return Quantity < 0;
    }

    /// <summary>
    /// Gets the absolute quantity value
    /// </summary>
    /// <returns>The absolute value of the quantity</returns>
    public decimal GetAbsoluteQuantity()
    {
        return Math.Abs(Quantity);
    }
}