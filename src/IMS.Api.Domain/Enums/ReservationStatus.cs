namespace IMS.Api.Domain.Enums;

/// <summary>
/// Represents the status of a stock reservation
/// </summary>
public enum ReservationStatus
{
    /// <summary>
    /// Reservation is active and holding stock
    /// </summary>
    Active = 1,

    /// <summary>
    /// Reservation has been fulfilled/used
    /// </summary>
    Fulfilled = 2,

    /// <summary>
    /// Reservation has been manually cancelled
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Reservation has expired automatically
    /// </summary>
    Expired = 4,

    /// <summary>
    /// Reservation has been partially fulfilled
    /// </summary>
    PartiallyFulfilled = 5
}