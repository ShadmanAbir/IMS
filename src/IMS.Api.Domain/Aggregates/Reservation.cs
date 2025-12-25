using IMS.Api.Domain.Common;
using IMS.Api.Domain.Enums;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Aggregates;

/// <summary>
/// Reservation aggregate root representing a temporary allocation of stock that reduces available inventory
/// </summary>
public sealed class Reservation : SoftDeletableAggregateRoot<ReservationId>
{
    public VariantId VariantId { get; private set; }
    public WarehouseId WarehouseId { get; private set; }
    public decimal OriginalQuantity { get; private set; }
    public decimal CurrentQuantity { get; private set; }
    public decimal FulfilledQuantity { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public ReservationStatus Status { get; private set; }
    public string ReferenceNumber { get; private set; }
    public string? Notes { get; private set; }
    public TenantId TenantId { get; private set; }
    public UserId CreatedBy { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public UserId? UpdatedBy { get; private set; }
    public UserId? UsedBy { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public UserId? CancelledBy { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    // Convenience properties for backward compatibility
    public decimal Quantity => CurrentQuantity;
    public string? Reason => Notes;

    private Reservation(
        ReservationId id,
        VariantId variantId,
        WarehouseId warehouseId,
        decimal quantity,
        DateTime expiresAtUtc,
        string referenceNumber,
        string? notes,
        TenantId tenantId,
        UserId createdBy) : base(id)
    {
        VariantId = variantId;
        WarehouseId = warehouseId;
        OriginalQuantity = quantity;
        CurrentQuantity = quantity;
        FulfilledQuantity = 0;
        ExpiresAtUtc = expiresAtUtc;
        Status = ReservationStatus.Active;
        ReferenceNumber = referenceNumber;
        Notes = notes;
        TenantId = tenantId;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static Reservation Create(
        VariantId variantId,
        WarehouseId warehouseId,
        decimal quantity,
        DateTime expiresAtUtc,
        string referenceNumber,
        string? notes,
        TenantId tenantId,
        UserId createdBy)
    {
        ValidateReservationData(variantId, warehouseId, quantity, expiresAtUtc, referenceNumber, tenantId, createdBy);

        var reservationId = ReservationId.CreateNew();
        return new Reservation(
            reservationId,
            variantId,
            warehouseId,
            quantity,
            expiresAtUtc,
            referenceNumber,
            notes,
            tenantId,
            createdBy);
    }

    public void ModifyQuantity(decimal newQuantity, UserId updatedBy)
    {
        if (Status != ReservationStatus.Active)
            throw new InvalidOperationException($"Cannot modify quantity for reservation with status {Status}");

        if (newQuantity <= 0)
            throw new ArgumentException("Reservation quantity must be positive", nameof(newQuantity));

        if (newQuantity < FulfilledQuantity)
            throw new ArgumentException("New quantity cannot be less than already fulfilled quantity", nameof(newQuantity));

        CurrentQuantity = newQuantity;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;

        // Update status if partially fulfilled
        if (FulfilledQuantity > 0 && FulfilledQuantity < CurrentQuantity)
        {
            Status = ReservationStatus.PartiallyFulfilled;
        }
    }

    public void ExtendExpiry(DateTime newExpiryUtc, UserId updatedBy)
    {
        if (Status != ReservationStatus.Active && Status != ReservationStatus.PartiallyFulfilled)
            throw new InvalidOperationException($"Cannot extend expiry for reservation with status {Status}");

        if (newExpiryUtc <= DateTime.UtcNow)
            throw new ArgumentException("New expiry date must be in the future", nameof(newExpiryUtc));

        if (newExpiryUtc <= ExpiresAtUtc)
            throw new ArgumentException("New expiry date must be later than current expiry", nameof(newExpiryUtc));

        ExpiresAtUtc = newExpiryUtc;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Fulfill(decimal quantity, UserId updatedBy)
    {
        if (Status != ReservationStatus.Active && Status != ReservationStatus.PartiallyFulfilled)
            throw new InvalidOperationException($"Cannot fulfill reservation with status {Status}");

        if (quantity <= 0)
            throw new ArgumentException("Fulfill quantity must be positive", nameof(quantity));

        if (FulfilledQuantity + quantity > CurrentQuantity)
            throw new ArgumentException("Fulfill quantity exceeds available reservation quantity", nameof(quantity));

        FulfilledQuantity += quantity;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;

        // Update status based on fulfillment
        if (FulfilledQuantity >= CurrentQuantity)
        {
            Status = ReservationStatus.Fulfilled;
        }
        else if (FulfilledQuantity > 0)
        {
            Status = ReservationStatus.PartiallyFulfilled;
        }
    }

    public void Cancel(UserId updatedBy, string? reason = null)
    {
        if (Status != ReservationStatus.Active && Status != ReservationStatus.PartiallyFulfilled)
            throw new InvalidOperationException($"Cannot cancel reservation with status {Status}");

        Status = ReservationStatus.Cancelled;
        UpdatedBy = updatedBy;
        CancelledBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
        CancelledAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? reason : $"{Notes}; Cancelled: {reason}";
        }
    }

    public void UpdateReason(string reason, UserId updatedBy)
    {
        Notes = reason;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Expire()
    {
        if (Status != ReservationStatus.Active && Status != ReservationStatus.PartiallyFulfilled)
            throw new InvalidOperationException($"Cannot expire reservation with status {Status}");

        if (DateTime.UtcNow < ExpiresAtUtc)
            throw new InvalidOperationException("Cannot expire reservation before expiry date");

        Status = ReservationStatus.Expired;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes, UserId updatedBy)
    {
        Notes = notes;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsActive()
    {
        return Status == ReservationStatus.Active || Status == ReservationStatus.PartiallyFulfilled;
    }

    public bool IsExpired()
    {
        return Status == ReservationStatus.Expired || DateTime.UtcNow >= ExpiresAtUtc;
    }

    public bool CanBeFulfilled()
    {
        return IsActive() && !IsExpired() && GetRemainingQuantity() > 0;
    }

    public bool CanBeModified()
    {
        return IsActive() && !IsExpired();
    }

    public decimal GetRemainingQuantity()
    {
        return CurrentQuantity - FulfilledQuantity;
    }

    public decimal GetFulfillmentPercentage()
    {
        if (CurrentQuantity == 0)
            return 0;

        return (FulfilledQuantity / CurrentQuantity) * 100;
    }

    public TimeSpan GetTimeUntilExpiry()
    {
        var timeUntilExpiry = ExpiresAtUtc - DateTime.UtcNow;
        return timeUntilExpiry > TimeSpan.Zero ? timeUntilExpiry : TimeSpan.Zero;
    }

    public bool IsExpiringWithin(TimeSpan timeSpan)
    {
        return GetTimeUntilExpiry() <= timeSpan;
    }

    private static void ValidateReservationData(
        VariantId variantId,
        WarehouseId warehouseId,
        decimal quantity,
        DateTime expiresAtUtc,
        string referenceNumber,
        TenantId tenantId,
        UserId createdBy)
    {
        if (variantId == null)
            throw new ArgumentNullException(nameof(variantId));

        if (warehouseId == null)
            throw new ArgumentNullException(nameof(warehouseId));

        if (quantity <= 0)
            throw new ArgumentException("Reservation quantity must be positive", nameof(quantity));

        if (expiresAtUtc <= DateTime.UtcNow)
            throw new ArgumentException("Expiry date must be in the future", nameof(expiresAtUtc));

        if (string.IsNullOrWhiteSpace(referenceNumber))
            throw new ArgumentException("Reference number is required", nameof(referenceNumber));

        if (referenceNumber.Length > 100)
            throw new ArgumentException("Reference number cannot exceed 100 characters", nameof(referenceNumber));

        if (tenantId == null)
            throw new ArgumentNullException(nameof(tenantId));

        if (createdBy == null)
            throw new ArgumentNullException(nameof(createdBy));
    }
}